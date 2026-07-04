namespace Jellyfin.Plugin.PlaybackTrace.Models;

/// <summary>
/// A single playback timeline event.
/// </summary>
public sealed class PlaybackTraceEvent
{
    /// <summary>
    /// Gets or sets the event id.
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the session key.
    /// </summary>
    public string SessionKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Jellyfin session id.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the play session id.
    /// </summary>
    public string? PlaySessionId { get; set; }

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
    /// Gets or sets the item type.
    /// </summary>
    public string? ItemType { get; set; }

    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Gets or sets the device id.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets or sets the media position in seconds.
    /// </summary>
    public double PositionSeconds { get; set; }

    /// <summary>
    /// Gets or sets the previous media position in seconds.
    /// </summary>
    public double? FromSeconds { get; set; }

    /// <summary>
    /// Gets or sets the target media position in seconds.
    /// </summary>
    public double? ToSeconds { get; set; }

    /// <summary>
    /// Gets or sets the wall-clock delta since the previous event.
    /// </summary>
    public double? WallDeltaSeconds { get; set; }

    /// <summary>
    /// Gets or sets the media-position delta since the previous event.
    /// </summary>
    public double? PositionDeltaSeconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether playback is paused.
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Jellyfin generated the event automatically.
    /// </summary>
    public bool IsAutomated { get; set; }
}

