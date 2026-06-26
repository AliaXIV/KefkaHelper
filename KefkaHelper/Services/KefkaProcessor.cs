using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel;
using Lumina.Text;

namespace KefkaHelper.Services;

// 1. we want to watch for the forsaken debuff and attach new status that represents the stored debuff
// 2. we want to check for valid blackhole statuses and create waymark suggestions

public enum ForsakenMechanicType
{
    Stack,
    Circle,
    Cone,
}

public class KefkaProcessor : IDisposable
{
    private const uint ActionIdFutureEnd = 47826;
    private const uint ActionIdPastEnd = 47827;
    private const uint ActionIdEarthquake1 = 50545;
    private const uint ActionIdEarthquake2 = 50546;


    private const int StatusIdForsakenStack = 5084;
    private const int StatusIdForsakenCircle = 5085;
    private const int StatusIdForsakenCone = 5086;

    private const uint NpcBaseIdKefkaP2 = 19506;
    private const uint NpcBaseIdKefkaP2Clone = 19513;

    public Dictionary<int, PlayerMarker>? CachedBlackholeMarkers;
    public event Action<Dictionary<int, PlayerMarker>> OnBlackholeMarkersUpdated;
    
    private Plugin _plugin;
    private ExcelSheet<Lumina.Excel.Sheets.Action> _actionSheet;
    private List<(DateTime when, Action handler)> _scheduledActions = new();


    public bool IsForsakenEnabled { get; private set; }

    public KefkaProcessor(Plugin plugin)
    {
        _plugin = plugin;
        _actionSheet = Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>();


        Plugin.Framework.Update += Update;
        _plugin.BattleProcessor.OnActorCast += HandleActorCast;
    }


    public void Dispose()
    {
        Plugin.Framework.Update -= Update;
        _plugin.BattleProcessor.OnActorCast -= HandleActorCast;
    }

    private void Update(IFramework framework)
    {
        var now = DateTime.UtcNow;
        for (var i = _scheduledActions.Count - 1; i >= 0; i--)
        {
            var action = _scheduledActions[i];
            if (now > action.when)
            {
                action.handler.Invoke();
                _scheduledActions.RemoveAt(i);
            }
        }

        if (IsForsakenEnabled)
        {
            CheckForsaken();
        }
    }

    private void HandleActorCast(uint sourceId, ActorCastPacket data)
    {

        var obj = Plugin.ObjectTable.SearchById(sourceId);
        if (obj is {ObjectKind: ObjectKind.BattleNpc})
        {
            Plugin.Log.Info(
                $"[cast] {obj.Name.TextValue} {_actionSheet.GetRow(data.ActionId).Name.ExtractText()} sourceBaseId: {obj.BaseId} castId: {data.ActionId} castTime: {data.CastTime}");
        }

        if (IsForsakenEnabled)
        {
            if (data.ActionId is ActionIdPastEnd or ActionIdFutureEnd)
            {
                var action = _actionSheet.GetRow(data.ActionId);
                _plugin.ForsakenWindow.LastEndCastId = data.ActionId;

                var message = new SeStringBuilder()
                              .PushColorRgba(new Vector4(0.9f, 0.1f, 0.8f, 1.0f))
                              .Append(action.Name)
                              .PopColor()
                              .ToReadOnlySeString();
                Plugin.ChatGui.Print(message);
            }
        }

        if (data.ActionId == ActionIdEarthquake1)
        {
            DoAfter(
                data.CastTime + 5.0f,
                () =>
                {
                    CachedBlackholeMarkers = GetBlackholeMarkers();
                    OnBlackholeMarkersUpdated?.Invoke(CachedBlackholeMarkers);
                    Plugin.ChatGui.Print("Blackhole ready");
                    Plugin.Log.Info("Blackhole ready");
                });
        }
    }


    private void CheckForsaken()
    {
        var localStatuses = GetLocalStatuses();
        var forsakenDebuff =
            localStatuses?.FirstOrDefault(s => s!.StatusId is StatusIdForsakenStack or StatusIdForsakenCircle or StatusIdForsakenCone,
                                          null);
        if (forsakenDebuff != null)
        {
            _plugin.ForsakenWindow.IsForsakenActive = true;
            _plugin.ForsakenWindow.Mechanic = forsakenDebuff.StatusId switch
            {
                StatusIdForsakenStack => ForsakenMechanicType.Stack,
                StatusIdForsakenCircle => ForsakenMechanicType.Circle,
                StatusIdForsakenCone => ForsakenMechanicType.Cone,
                _ => throw new ArgumentOutOfRangeException()
            };
            if (!_plugin.ForsakenWindow.IsOpen)
            {
                _plugin.ForsakenWindow.IsOpen = true;
            }
        }
        else
        {
            _plugin.ForsakenWindow.IsForsakenActive = false;
            if (_plugin.ForsakenWindow is {IsOpen: true, IsPreview: false})
            {
                _plugin.ForsakenWindow.IsOpen = false;
                _plugin.ForsakenWindow.LastEndCastId = null;
            }
        }
    }

    public Dictionary<int, PlayerMarker> GetBlackholeMarkers()
    {
        const int firstInLineId = 3004;
        const int secondInLineId = 3005;
        const int thirdInLineId = 3006;
        const int accretionId = 1604;
        const int primordialCrustId = 5454;

        var result = new Dictionary<int, PlayerMarker>();
        var partyList = Plugin.OrderedPartyList;

        for (var partyListIdx = 0; partyListIdx < partyList.Count; partyListIdx++)
        {
            var player = partyList[partyListIdx];

            if (player.Statuses.Any(s => s.StatusId == firstInLineId))
            {
                result[partyListIdx] = player.Statuses.Any(s => s.StatusId == accretionId) ? PlayerMarker.Ignore1 :
                                       IsSupport(player) ? PlayerMarker.Bind1 : PlayerMarker.Attack1;
            }
            else if (player.Statuses.Any(s => s.StatusId == secondInLineId))
            {
                result[partyListIdx] = player.Statuses.Any(s => s.StatusId == accretionId) ? PlayerMarker.Ignore2 :
                                       IsSupport(player) ? PlayerMarker.Bind2 : PlayerMarker.Attack2;
            }
            else if (player.Statuses.Any(s => s.StatusId == thirdInLineId))
            {
                result[partyListIdx] = IsSupport(player) ? PlayerMarker.Bind3 : PlayerMarker.Attack3;
            }
        }
        return result;
    }

    private static bool IsSupport(IPartyMember player)
    {
        return player.ClassJob.Value.JobType is 1 or 2 or 6;
    }

    private unsafe StatusList? GetLocalStatuses()
    {
        var character = Control.Instance()->LocalPlayer;
        return character == null ? null : StatusList.CreateStatusListReference((nint)character->GetStatusManager())!;
    }

    public void SetForsakenEnabled(bool enabled)
    {

        IsForsakenEnabled = enabled;
    }

    private void DoAfter(float after, Action action)
    {
        var when = DateTime.UtcNow.AddSeconds(after);
        _scheduledActions.Add((when, action));
    }
}
