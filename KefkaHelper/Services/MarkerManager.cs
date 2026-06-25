using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;

namespace KefkaHelper.Services;

public enum PlayerMarker
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
    private bool isSchedulerActive = false;
    private readonly Queue<(TimeSpan delay, int partyListIndex, PlayerMarker marker)> scheduledMarkers = new();
    private TimeSpan currentTimer = TimeSpan.Zero;

    public MarkerManager()
    {
    }

    public void Dispose()
    {
        Plugin.Framework.Update -= Update;
    }

    public static void MarkPlayer(int playerIdx, PlayerMarker marker)
    {
        Plugin.Log.Info($"marking player {playerIdx} with {marker}");
        CommandExecutor.Instance.ExecuteCommand($"/mk {GetMarkerKey(marker)} <{playerIdx + 1}>");
    }

    public static void UnmarkPlayer(int playerIdx)
    {
        CommandExecutor.Instance.ExecuteCommand($"/mk clear <{playerIdx + 1}>");
    }

    private static string GetMarkerKey(PlayerMarker marker)
    {
        return marker switch
        {
            PlayerMarker.Attack1 => "attack1",
            PlayerMarker.Attack2 => "attack2",
            PlayerMarker.Attack3 => "attack3",
            PlayerMarker.Attack4 => "attack4",
            PlayerMarker.Attack5 => "attack5",
            PlayerMarker.Attack6 => "attack6",
            PlayerMarker.Attack7 => "attack7",
            PlayerMarker.Attack8 => "attack8",
            PlayerMarker.Bind1 => "bind1",
            PlayerMarker.Bind2 => "bind2",
            PlayerMarker.Bind3 => "bind3",
            PlayerMarker.Ignore1 => "ignore1",
            PlayerMarker.Ignore2 => "ignore2",
            _ => throw new ArgumentOutOfRangeException(nameof(marker), marker, null)
        };
    }

    public void MarkMultipleStaggered((int, PlayerMarker)[] list, float baseDelay)
    {
        foreach (var (partyListIndex, markerType) in list)
        {
            var delay = (float)(baseDelay + (Random.Shared.NextDouble() * 0.5f));
            ScheduleMark(partyListIndex, markerType, delay);
        }
    }

    private void ScheduleMark(int playerIdx, PlayerMarker marker, float delay)
    {
        scheduledMarkers.Enqueue((currentTimer.Add(TimeSpan.FromSeconds(delay)), playerIdx, marker));
        if (!isSchedulerActive)
        {
            currentTimer = TimeSpan.Zero;
            isSchedulerActive = true;
            Plugin.Framework.Update += Update;
        }
    }

    private void Update(IFramework framework)
    {
        if (scheduledMarkers.Count == 0)
        {
            Plugin.Framework.Update -= Update;
            isSchedulerActive = false;
            return;
        }

        currentTimer += framework.UpdateDelta;

        var scheduled = scheduledMarkers.Peek();
        if (currentTimer > scheduled.delay)
        {
            currentTimer = TimeSpan.Zero;
            MarkPlayer(scheduled.partyListIndex, scheduled.marker);
            scheduledMarkers.Dequeue();
        }
    }
}
