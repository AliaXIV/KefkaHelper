using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace KefkaHelper.Windows;

public class MainWindow : Window, IDisposable
{
    private struct BlackholePartyEntry(int partyListIndex, string name, MarkerType? markerAssignment)
    {
        public int PartyListIndex = partyListIndex;
        public string Name = name;
        public MarkerType? MarkerAssignment = markerAssignment;
    }

    private readonly Plugin plugin;
    private List<BlackholePartyEntry> _blackholePartyData = new();

    public MainWindow(Plugin plugin)
        : base("Kefka Helper##KefkaMain", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(200, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        // Size = new Vector2(300, 300);

        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.BeginGroup();

        if (ImGui.Button("Check for blackhole"))
        {
            CheckForBlackhole();
        }

        foreach (var player in _blackholePartyData)
        {
            ImGui.PushID(player.PartyListIndex);

            ImGui.Text(player.Name);
            ImGui.SameLine(120);
            ImGui.Text(player.MarkerAssignment.HasValue ? player.MarkerAssignment.Value.ToString() : "---");
            ImGui.SameLine(180);
            if (ImGui.SmallButton($"Mark"))
            {
                Plugin.Framework.RunOnTick(() =>
                {
                    plugin.MarkerManager.MarkPlayer(
                        player.PartyListIndex,
                        player.MarkerAssignment ?? MarkerType.Attack4);
                });
            }

            ImGui.PopID();
        }

        ImGui.Separator();
        if (ImGui.Button("Mark All"))
        {
            var list = _blackholePartyData.Where(e => e.MarkerAssignment.HasValue)
                                          .Select(e => (e.PartyListIndex, e.MarkerAssignment!.Value))
                                          .ToArray();

            Plugin.Framework.RunOnTick(() =>
            {
                plugin.MarkerManager.MarkMultipleStaggered(list, 0.5f);
            });
            
        }

        ImGui.EndGroup();

        ImGui.Separator();

        if (ImGui.Button("Log statuses"))
        {
            plugin.LogDebuffStatuses();
        }

        if (ImGui.Button("Debug assignment"))
        {
            DebugBlackhole();
        }
    }

    private void CheckForBlackhole()
    {
        var result = new List<BlackholePartyEntry>();
        var markers = plugin.StatusProcessor.GetBlackholeMarkers();

        var partyList = Plugin.OrderedPartyList;

        for (var i = 0; i < partyList.Count; i++)
        {
            Plugin.Log.Info($"checking player {i}");
            var player = partyList[i];

            Plugin.Log.Info($"player {i}, {player.Name}");

            MarkerType? assignedMarker = null;
            if (markers.TryGetValue(i, out var marker))
            {
                assignedMarker = marker;
            }

            result.Add(new BlackholePartyEntry(i, player.Name.ToString(), assignedMarker));
        }

        _blackholePartyData = result;
    }

    private void DebugBlackhole()
    {
        var result = new List<BlackholePartyEntry>();
        var markers = new Dictionary<int, MarkerType>()
        {
            { 0, MarkerType.Attack1 },
            { 1, MarkerType.Attack2 },
            { 2, MarkerType.Attack3 },
            { 3, MarkerType.Bind1 },
            { 4, MarkerType.Bind2 },
            { 5, MarkerType.Bind3 },
            { 6, MarkerType.Ignore1 },
            { 7, MarkerType.Ignore2 },
        };

        var partyList = Plugin.OrderedPartyList;
        for (var i = 0; i < partyList.Count; i++)
        {
            Plugin.Log.Info($"checking player {i}");
            var player = partyList[i];

            Plugin.Log.Info($"player {i}, {player.Name}");
            MarkerType? assignedMarker = markers[i];
            result.Add(new BlackholePartyEntry(i, player.Name.ToString(), assignedMarker));
        }

        _blackholePartyData = result;
    }
}
