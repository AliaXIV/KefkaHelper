using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Serilog;

namespace KefkaHelper;

// 1. we want to watch for the forsaken debuff and attach new status that represents the stored debuff
// 2. we want to check for valid blackhole statuses and create waymark suggestions

public enum ForsakenMechanicType
{
    Stack, 
    Circle,
    Cone,
}

public class StatusProcessor : IDisposable
{
    private Plugin _plugin;
    
    public bool IsForsakenEnabled { get; private set; }
    
    public StatusProcessor(Plugin plugin)
    {
        _plugin = plugin;
        Plugin.Framework.Update += Update;
    }

    public void Dispose()
    {
        Plugin.Framework.Update -= Update;
    }

    private void Update(IFramework framework)
    {
        if (IsForsakenEnabled)
        {
            CheckForsaken();
        }      
    }

    private void CheckForsaken()
    {
        const int forsakenStackId = 5084;
        const int forsakenCircleId = 5085;
        const int forsakenConeId = 5086;

        var localStatuses =GetLocalStatuses();
        var forsakenDebuff = localStatuses?.FirstOrDefault(s => s!.StatusId is forsakenStackId or forsakenCircleId or forsakenConeId, null);
        if (forsakenDebuff != null)
        {
            _plugin.ForsakenWindow.IsForsakenActive = true;
            _plugin.ForsakenWindow.Mechanic = forsakenDebuff.StatusId switch
            {
                forsakenStackId => ForsakenMechanicType.Stack,
                forsakenCircleId => ForsakenMechanicType.Circle,
                forsakenConeId => ForsakenMechanicType.Cone,
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
            if (_plugin.ForsakenWindow is { IsOpen: true, IsPreview: false })
            {
                _plugin.ForsakenWindow.IsOpen = false;
                _plugin.ForsakenWindow.LastEndCastId = null;
            }
        }
        
        const uint p2KefkaBaseId = 19506;
        const uint p2KefkaCloneBaseId = 19513;
        
        const uint futureEndId = 47826;
        const uint pastEndId = 47827;

        
        var p2KefkaGameObject = Plugin.ObjectTable.FirstOrDefault(o => o.BaseId == p2KefkaBaseId);
        if (p2KefkaGameObject is IBattleNpc npc)
        {
            if (npc is { IsCasting: true, CastActionId: futureEndId or pastEndId })
            {
                _plugin.ForsakenWindow.LastEndCastId = npc.CastActionId;
            }
        }
    }
    
    public Dictionary<int, MarkerType> GetBlackholeMarkers()
    {
        const int firstInLineId = 3004;
        const int secondInLineId = 3005;
        const int thirdInLineId = 3006;
        const int accretionId = 1604;
        const int primordialCrustId = 5454;
        
        var result = new Dictionary<int, MarkerType>();
        var partyList = Plugin.OrderedPartyList;
        
        for (var partyListIdx = 0; partyListIdx < partyList.Count; partyListIdx++)
        {
            var player = partyList[partyListIdx];

            if (player.Statuses.Any(s => s.StatusId == firstInLineId))
            {
                result[partyListIdx] = player.Statuses.Any(s => s.StatusId == accretionId) ? MarkerType.Ignore1 :
                                       IsSupport(player) ? MarkerType.Bind1 : MarkerType.Attack1;
            } else if (player.Statuses.Any(s => s.StatusId == secondInLineId))
            {
                result[partyListIdx] = player.Statuses.Any(s => s.StatusId == accretionId) ? MarkerType.Ignore2 :
                                       IsSupport(player) ? MarkerType.Bind2 : MarkerType.Attack2;
            } else if (player.Statuses.Any(s => s.StatusId == thirdInLineId))
            {
                result[partyListIdx] = IsSupport(player) ? MarkerType.Bind3 : MarkerType.Attack3;
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
}
