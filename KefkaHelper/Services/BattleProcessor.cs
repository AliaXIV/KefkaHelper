using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using Lumina.Excel.Sheets;

namespace KefkaHelper.Services;

public unsafe class BattleProcessor : IDisposable
{
    public enum StatusChange
    {
        Added,
        Removed,
        Updated,
    }

    public record StatusData(uint StatusId, IStatus Status);
    
    public delegate void ActorCastDelegate(uint sourceId, ActorCastPacket data);
    public event ActorCastDelegate? OnActorCast;

    public delegate void ActorStatusEffectChangeDelegate(uint sourceId, StatusChange change, StatusData status);
    public event ActorStatusEffectChangeDelegate? OnActorStatusEffectChange;
    
    private Dictionary<uint, List<StatusData>> _statusSnapshot = new();

    private delegate void ActorCastHookDelegate(uint sourceId, ActorCastPacket* packet);
    [Signature("40 53 57 48 81 EC ?? ?? ?? ?? 48 8B FA 8B D1", DetourName = nameof(ActorCastDetour))]
    private Hook<ActorCastHookDelegate>? _actorCastHook = null!;

    public BattleProcessor()
    {
        Plugin.GameInteropProvider.InitializeFromAttributes(this);
        _actorCastHook?.Enable();

        Plugin.Framework.Update += Update;
        Plugin.ClientState.TerritoryChanged += HandleTerritoryChanged;
    }

    public void Dispose()
    {
        _actorCastHook?.Dispose();
        Plugin.Framework.Update -= Update;
        Plugin.ClientState.TerritoryChanged -= HandleTerritoryChanged;
    }

    private void Update(IFramework framework)
    {
        foreach (var chara in Plugin.ObjectTable.PlayerObjects)
        {
            var entityId = chara.EntityId;
            var snapshot = _statusSnapshot.GetValueOrDefault(entityId, []);
            var current = chara.StatusList.Select(s => new StatusData(s.StatusId, s)).ToList();

            // so we iterate over current set
            // if something is there and is not in the snapshot its a gain
            // then we iterate again to check for the ones that are in snapshot but not in current to have loses
            
            var added = current.Where(cur => snapshot.All(snap => snap.StatusId != cur.StatusId)).ToArray();
            var removed = snapshot.Where(s => current.All(cur => cur.StatusId != s.StatusId)).ToArray();
            var updated = current.Where(cur =>
            {
                var snapStatus = snapshot.FirstOrDefault(snap => snap?.StatusId == cur.StatusId, null);
                return snapStatus != null && cur.Status.Param != snapStatus.Status.Param;
            }).ToArray();
            
            foreach (var statusData in added)
            {
                OnActorStatusEffectChange?.Invoke(entityId,  StatusChange.Added, statusData);
            }
            
            foreach (var statusData in removed)
            {
                OnActorStatusEffectChange?.Invoke(entityId,  StatusChange.Removed, statusData);
            }
            
            foreach (var statusData in updated)
            {
                OnActorStatusEffectChange?.Invoke(entityId,  StatusChange.Updated, statusData);
            }
            _statusSnapshot[entityId] = current;
        }
    }

    private void HandleTerritoryChanged(uint territoryId)
    {
        var territory = Plugin.DataManager.GetExcelSheet<TerritoryType>().GetRow(territoryId);
        Plugin.Log.Debug($"Changed territory: {territory.Name.ExtractText()}");
        _statusSnapshot.Clear();
    }
    
    
    private void ActorCastDetour(uint sourceId, ActorCastPacket* packet)
    {
        OnActorCast?.Invoke(sourceId, *packet);
        _actorCastHook!.Original(sourceId, packet);
    }
}
