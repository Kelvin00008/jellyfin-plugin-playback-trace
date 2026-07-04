using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PlaybackTrace.Configuration;

/// <summary>
/// Playback Trace plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the timeline jump threshold used to infer seek events.
    /// </summary>
    public int SeekThresholdSeconds { get; set; } = 15;

    /// <summary>
    /// Gets or sets the minimum segment duration saved to the watched segment file.
    /// </summary>
    public int MinimumSegmentSeconds { get; set; } = 3;

    /// <summary>
    /// Gets or sets how often normal playback progress samples are written.
    /// </summary>
    public int SampleIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the default number of days included in exports.
    /// </summary>
    public int DefaultExportDays { get; set; } = 30;
}

