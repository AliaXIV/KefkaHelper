using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace KefkaHelper;

public enum MarkerType
{
    Attack1,
    Attack2,
    Attack3,
    Attack4,
    Attack5,
    Attack6,
    Attack7,
    Attack8,
    Bind1,
    Bind2,
    Bind3,
    Ignore1,
    Ignore2,
}

public class MarkerManager : IDisposable
{
    private bool _isSchedulerActive = false;
    private Queue<(TimeSpan delay, int partyListIndex, MarkerType marker)> _scheduledMarkers = new();
    private TimeSpan _currentTimer = TimeSpan.Zero;

    public MarkerManager() { }

    public void Dispose()
    {
        Plugin.Framework.Update -= Update;
    }

    public void MarkPlayer(int playerIdx, MarkerType marker)
    {
        Plugin.Log.Info($"marking player {playerIdx} with {marker}");
        CommandExecutor.ExecuteCommand($"/mk {GetMarkerKey(marker)} <{playerIdx + 1}>");
    }

    public void UnmarkPlayer(int playerIdx)
    {
        CommandExecutor.ExecuteCommand($"/mk clear <{playerIdx + 1}>");
    }

    private static string GetMarkerKey(MarkerType marker)
    {
        return marker switch
        {
            MarkerType.Attack1 => "attack1",
            MarkerType.Attack2 => "attack2",
            MarkerType.Attack3 => "attack3",
            MarkerType.Attack4 => "attack4",
            MarkerType.Attack5 => "attack5",
            MarkerType.Attack6 => "attack6",
            MarkerType.Attack7 => "attack7",
            MarkerType.Attack8 => "attack8",
            MarkerType.Bind1 => "bind1",
            MarkerType.Bind2 => "bind2",
            MarkerType.Bind3 => "bind3",
            MarkerType.Ignore1 => "ignore1",
            MarkerType.Ignore2 => "ignore2",
            _ => throw new ArgumentOutOfRangeException(nameof(marker), marker, null)
        };
    }

    public void MarkMultipleStaggered((int, MarkerType)[] list, float baseDelay)
    {
        foreach (var (partyListIndex, markerType) in list)
        {
            var delay = (float)(baseDelay + (Random.Shared.NextDouble() * 0.5f));
            ScheduleMark(partyListIndex, markerType, delay);
        }
    }

    private void ScheduleMark(int playerIdx, MarkerType marker, float delay)
    {
        _scheduledMarkers.Enqueue((_currentTimer.Add(TimeSpan.FromSeconds(delay)), playerIdx, marker));
        if (!_isSchedulerActive)
        {
            _currentTimer = TimeSpan.Zero;
            _isSchedulerActive = true;
            Plugin.Framework.Update += Update;
        }
    }

    private void Update(IFramework framework)
    {
        if (_scheduledMarkers.Count == 0)
        {
            Plugin.Framework.Update -= Update;
            _isSchedulerActive = false;
            return;
        }

        _currentTimer += framework.UpdateDelta;

        var scheduled = _scheduledMarkers.Peek();
        if (_currentTimer > scheduled.delay)
        {
            _currentTimer = TimeSpan.Zero;
            MarkPlayer(scheduled.partyListIndex, scheduled.marker);
            _scheduledMarkers.Dequeue();
        }
    }
}
