using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;

namespace KefkaHelper;

public enum MarkerType
{
    Attack1,
    Attack2,
    Attack3,
    Attack4,
    Bind1,
    Bind2,
    Bind3,
    Bind4,
    Ignore1,
    Ignore2,
    Ignore3,
    Ignore4,
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
        Plugin.ChatGui.Print($"/e mk {GetMarkerKey(marker)} <{playerIdx + 1}>");
    }

    public void UnmarkPlayer(int playerIdx)
    {
        Plugin.ChatGui.Print($"/e mk clear <{playerIdx + 1}>");
    }

    private static string GetMarkerKey(MarkerType marker)
    {
        return marker switch
        {
            MarkerType.Attack1 => "attack1",
            MarkerType.Attack2 => "attack2",
            MarkerType.Attack3 => "attack3",
            MarkerType.Attack4 => "attack4",
            MarkerType.Bind1 => "bind1",
            MarkerType.Bind2 => "bind2",
            MarkerType.Bind3 => "bind3",
            MarkerType.Bind4 => "bind4",
            MarkerType.Ignore1 => "ignore1",
            MarkerType.Ignore2 => "ignore2",
            MarkerType.Ignore3 => "ignore3",
            MarkerType.Ignore4 => "ignore4",
            _ => throw new ArgumentOutOfRangeException(nameof(marker), marker, null)
        };
    }

    public void MarkMultipleStaggered((int, MarkerType)[] list, float baseDelay)
    {
        foreach (var (partyListIndex, markerType) in list)
        {
            var delay = baseDelay + (Random.Shared.NextDouble() * 1.0f);
            
            ScheduleMark(partyListIndex, markerType, 1.0f);
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
        if (scheduled.delay > _currentTimer)
        {
            _currentTimer = TimeSpan.Zero;
            MarkPlayer(scheduled.partyListIndex, scheduled.marker);
            _scheduledMarkers.Dequeue();
        }
    }
}
