namespace Jellyfin.Plugin.PlaybackTrace.Models;

/// <summary>
/// A continuous timeline range that was actually played.
/// </summary>
public sealed class WatchedSegment
{
    /// <summary>
    /// Gets or sets the segment id.
    /// </summary>
    public string SegmentId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the session key.
    /// </summary>
    public string SessionKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the item id.
    /// </summary>
    public string? ItemId { get; set; }

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string? ItemName { get; set; }

    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets or sets the segment media start in seconds.
    /// </summary>
    public double StartSeconds { get; set; }

    /// <summary>
    /// Gets or sets the segment media end in seconds.
    /// </summary>
    public double EndSeconds { get; set; }

    /// <summary>
    /// Gets or sets the wall-clock seconds spent in this segment.
    /// </summary>
    public double WatchedWallSeconds { get; set; }

    /// <summary>
    /// Gets or sets why the segment was closed.
    /// </summary>
    public string ClosedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the wall-clock time when the segment started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the wall-clock time when the segment ended.
    /// </summary>
    public DateTimeOffset EndedAt { get; set; }
}

