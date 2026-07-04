using Jellyfin.Plugin.PlaybackTrace.Configuration;
using Jellyfin.Plugin.PlaybackTrace.Models;

namespace Jellyfin.Plugin.PlaybackTrace.Services;

internal static class PlaybackTraceAnalyzer
{
    public static PlaybackTraceEvent BuildEvent(
        string eventType,
        PlaybackSnapshot snapshot,
        TraceSessionState? state = null,
        double? fromSeconds = null,
        double? toSeconds = null)
    {
        return new PlaybackTraceEvent
        {
            EventType = eventType,
            Timestamp = snapshot.Timestamp,
            SessionKey = snapshot.SessionKey,
            SessionId = snapshot.SessionId,
            PlaySessionId = snapshot.PlaySessionId,
            UserId = snapshot.UserId,
            UserName = snapshot.UserName,
            ItemId = snapshot.ItemId,
            ItemName = snapshot.ItemName,
            ItemType = snapshot.ItemType,
            ClientName = snapshot.ClientName,
            DeviceId = snapshot.DeviceId,
            DeviceName = snapshot.DeviceName,
            PositionSeconds = snapshot.PositionSeconds,
            FromSeconds = fromSeconds,
            ToSeconds = toSeconds,
            WallDeltaSeconds = state is null ? null : (snapshot.Timestamp - state.LastWallTime).TotalSeconds,
            PositionDeltaSeconds = state is null ? null : snapshot.PositionSeconds - state.LastPositionSeconds,
            IsPaused = snapshot.IsPaused,
            IsAutomated = snapshot.IsAutomated
        };
    }

    public static bool IsSeek(TraceSessionState state, PlaybackSnapshot snapshot, PluginConfiguration configuration)
    {
        var wallDelta = Math.Max(0, (snapshot.Timestamp - state.LastWallTime).TotalSeconds);
        var positionDelta = snapshot.PositionSeconds - state.LastPositionSeconds;
        var expectedPositionDelta = state.IsPaused ? 0 : wallDelta;
        var threshold = Math.Max(3, configuration.SeekThresholdSeconds);

        if (positionDelta < -threshold)
        {
            return true;
        }

        return Math.Abs(positionDelta - expectedPositionDelta) > threshold && Math.Abs(positionDelta) > threshold;
    }

    public static WatchedSegment? CloseSegment(TraceSessionState state, PlaybackSnapshot snapshot, string closedBy, PluginConfiguration configuration, double? endSeconds = null)
    {
        if (!state.OpenSegmentStartSeconds.HasValue || !state.OpenSegmentStartedAt.HasValue)
        {
            return null;
        }

        var start = state.OpenSegmentStartSeconds.Value;
        var startedAt = state.OpenSegmentStartedAt.Value;
        var end = Math.Max(start, endSeconds ?? state.LastPositionSeconds);
        var wallSeconds = Math.Max(0, (snapshot.Timestamp - state.OpenSegmentStartedAt.Value).TotalSeconds);
        var timelineSeconds = Math.Max(0, end - start);
        var minimum = Math.Max(1, configuration.MinimumSegmentSeconds);

        state.OpenSegmentStartSeconds = null;
        state.OpenSegmentStartedAt = null;

        if (timelineSeconds < minimum && wallSeconds < minimum)
        {
            return null;
        }

        return new WatchedSegment
        {
            SessionKey = state.Snapshot.SessionKey,
            UserId = state.Snapshot.UserId,
            UserName = state.Snapshot.UserName,
            ItemId = state.Snapshot.ItemId,
            ItemName = state.Snapshot.ItemName,
            ClientName = state.Snapshot.ClientName,
            DeviceName = state.Snapshot.DeviceName,
            StartSeconds = start,
            EndSeconds = end,
            WatchedWallSeconds = wallSeconds,
            ClosedBy = closedBy,
            StartedAt = startedAt,
            EndedAt = snapshot.Timestamp
        };
    }

    public static void OpenSegment(TraceSessionState state, PlaybackSnapshot snapshot)
    {
        state.OpenSegmentStartSeconds = snapshot.PositionSeconds;
        state.OpenSegmentStartedAt = snapshot.Timestamp;
    }
}
