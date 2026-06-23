using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace KefkaHelper;

// 1. we want to watch for the forsaken debuff and attach new status that represents the stored debuff
// 2. we want to check for valid blackhole statuses and create waymark suggestions

public class StatusProcessor : IDisposable
{
    public StatusProcessor()
    {
        Plugin.Framework.Update += Update;
    }

    public void Dispose()
    {
        Plugin.Framework.Update -= Update;
    }

    private void Update(IFramework framework)
    {
        // in update we only check local player and 
    }

    public Dictionary<int, MarkerType> GetBlackholeMarkers()
    {
        const int firstInLineId = 3004;
        const int secondInLineId = 3005;
        const int thirdInLineId = 3006;
        const int accretionId = 1604;
        const int primordialCrustId = 5454;
        
        var result = new Dictionary<int, MarkerType>();

        for (var partyListIdx = 0; partyListIdx < Plugin.PartyList.Length; partyListIdx++)
        {
            var player = Plugin.PartyList[partyListIdx];
            if (player == null)
            {
                continue;
            }

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

    private unsafe StatusList GetLocalStatuses()
    {
        var character = Control.Instance()->LocalPlayer;
        return StatusList.CreateStatusListReference((nint)character->GetStatusManager())!;
    }
}
