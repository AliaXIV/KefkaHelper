using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using KefkaHelper.Services;

namespace KefkaHelper.Windows;

public class MainWindow : Window, IDisposable
{
    private struct BlackholePartyEntry(int partyListIndex, string name, PlayerMarker? markerAssignment)
    {
        public int PartyListIndex = partyListIndex;
        public string Name = name;
        public PlayerMarker? MarkerAssignment = markerAssignment;
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

        plugin.KefkaProcessor.OnBlackholeMarkersUpdated += OnBlackholeMarkersUpdated;
    }


    public void Dispose()
    {
        plugin.KefkaProcessor.OnBlackholeMarkersUpdated -= OnBlackholeMarkersUpdated;
    }

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
                    MarkerManager.MarkPlayer(
                        player.PartyListIndex,
                        player.MarkerAssignment ?? PlayerMarker.Attack4);
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
        if (ImGui.Button("Mark Accretion"))
        {
            var list = _blackholePartyData
                       .Where(e => e.MarkerAssignment.HasValue)
                       .Select(e => (e.PartyListIndex, e.MarkerAssignment!.Value))
                       .Where(pair => pair.Value is PlayerMarker.Ignore1 or PlayerMarker.Ignore2)
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
        var markers = plugin.KefkaProcessor.GetBlackholeMarkers();
        UpdateBlackholeMarkers(markers);
    }

    private void DebugBlackhole()
    {
        var markers = new Dictionary<int, PlayerMarker>()
        {
            {0, PlayerMarker.Attack1},
            {1, PlayerMarker.Attack2},
            {2, PlayerMarker.Attack3},
            {3, PlayerMarker.Bind1},
            {4, PlayerMarker.Bind2},
            {5, PlayerMarker.Bind3},
            {6, PlayerMarker.Ignore1},
            {7, PlayerMarker.Ignore2},
        };
        UpdateBlackholeMarkers(markers);
    }

    private void OnBlackholeMarkersUpdated(Dictionary<int, PlayerMarker> markers)
    {
        UpdateBlackholeMarkers(markers);
    }

    private void UpdateBlackholeMarkers(Dictionary<int, PlayerMarker> markers)
    {
        var result = new List<BlackholePartyEntry>();

        var partyList = Plugin.OrderedPartyList;
        for (var i = 0; i < partyList.Count; i++)
        {
            Plugin.Log.Info($"checking player {i}");
            var player = partyList[i];

            Plugin.Log.Info($"player {i}, {player.Name}");

            PlayerMarker? assignedMarker = null;
            if (markers.TryGetValue(i, out var marker))
            {
                assignedMarker = marker;
            }
            result.Add(new BlackholePartyEntry(i, player.Name.ToString(), assignedMarker));
        }

        _blackholePartyData = result;
    }
}
