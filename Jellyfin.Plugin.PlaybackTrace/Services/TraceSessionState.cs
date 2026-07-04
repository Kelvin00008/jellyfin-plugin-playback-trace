namespace Jellyfin.Plugin.PlaybackTrace.Services;

internal sealed class TraceSessionState
{
    public required PlaybackSnapshot Snapshot { get; set; }

    public double LastPositionSeconds { get; set; }

    public DateTimeOffset LastWallTime { get; set; }

    public bool IsPaused { get; set; }

    public double? OpenSegmentStartSeconds { get; set; }

    public DateTimeOffset? OpenSegmentStartedAt { get; set; }

    public DateTimeOffset LastSampleAt { get; set; }
}

