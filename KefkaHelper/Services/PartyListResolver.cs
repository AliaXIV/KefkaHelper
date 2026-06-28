using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dalamud.Game.ClientState.Party;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace KefkaHelper.Services;

public static class PartyListResolver
{
    public record PartyEntry(uint Slot, IPartyMember Member);

    private static List<PartyEntry> PartyDataSnapshot = [];

    private static readonly Dictionary<string, int> PartyOrderSymbolMapping = new()
    {
        {"E090", 1},
        {"E091", 2},
        {"E092", 3},
        {"E093", 4},
        {"E094", 5},
        {"E095", 6},
        {"E096", 7},
        {"E097", 8},
        {"E0E0", 1},
        {"E0E1", 2},
        {"E0E2", 3},
        {"E0E3", 4},
        {"E0E4", 5},
        {"E0E5", 6},
        {"E0E6", 7},
        {"E0E7", 8},
    };

    public static unsafe List<PartyEntry> GetPartyListSnapshot()
    {

        var addon = Plugin.GameGui.GetAddonByName<AddonPartyList>("_PartyList");
        if (addon == null || !addon->IsVisible)
        {
            return PartyDataSnapshot;
        }

        var snapshot = new List<PartyEntry>();

        var uiMembers = addon->PartyMembers.ToArray().OrderBy(p =>
        {
            var bytes = Encoding.BigEndianUnicode.GetBytes(p.GroupSlotIndicator->NodeText.ExtractText());
            var charStr = Convert.ToHexString(bytes);
            return PartyOrderSymbolMapping.GetValueOrDefault(charStr, -1);
        }).ToArray();


        var partyList = Plugin.PartyList;

        for (uint i = 0; i < uiMembers.Length; i++)
        {
            var partyMember = uiMembers[i];
            var name = partyMember.Name->NodeText.ExtractText();
            name = string.Join(" ", name.Split(" ").Skip(1).ToArray()).TrimEnd('.');

            // we find actual party member that matches the uiMembers list

            var actualPlayer = partyList.FirstOrDefault(p => p?.Name.TextValue?.Contains(name) ?? false, null);
            if (actualPlayer != null)
            {
                snapshot.Add(new PartyEntry(i + 1, actualPlayer));
            }
        }
        PartyDataSnapshot = snapshot;
        return snapshot;
    }
}
