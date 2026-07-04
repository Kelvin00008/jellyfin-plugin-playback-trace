using System.Text.Json;
using Jellyfin.Plugin.PlaybackTrace.Models;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PlaybackTrace.Services;

/// <summary>
/// File-backed storage for playback trace records.
/// </summary>
public sealed class PlaybackTraceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly object _fileLock = new();
    private readonly ILogger<PlaybackTraceStore> _logger;
    private readonly string _eventsPath;
    private readonly string _segmentsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackTraceStore"/> class.
    /// </summary>
    /// <param name="configurationManager">Jellyfin configuration manager.</param>
    /// <param name="logger">Logger.</param>
    public PlaybackTraceStore(IServerConfigurationManager configurationManager, ILogger<PlaybackTraceStore> logger)
    {
        _logger = logger;
        var dataPath = Path.Combine(configurationManager.ApplicationPaths.DataPath, "playbacktrace");
        Directory.CreateDirectory(dataPath);
        _eventsPath = Path.Combine(dataPath, "trace-events.jsonl");
        _segmentsPath = Path.Combine(dataPath, "watched-segments.jsonl");
    }

    /// <summary>
    /// Appends an event.
    /// </summary>
    /// <param name="traceEvent">Trace event.</param>
    public void AppendEvent(PlaybackTraceEvent traceEvent)
    {
        AppendJsonLine(_eventsPath, traceEvent);
    }

    /// <summary>
    /// Appends a watched segment.
    /// </summary>
    /// <param name="segment">Watched segment.</param>
    public void AppendSegment(WatchedSegment segment)
    {
        AppendJsonLine(_segmentsPath, segment);
    }

    /// <summary>
    /// Reads events.
    /// </summary>
    /// <param name="since">Optional lower timestamp bound.</param>
    /// <param name="limit">Optional result limit.</param>
    /// <returns>Events.</returns>
    public IReadOnlyList<PlaybackTraceEvent> ReadEvents(DateTimeOffset? since = null, int? limit = null)
    {
        return ReadJsonLines<PlaybackTraceEvent>(_eventsPath, since, limit, item => item.Timestamp);
    }

    /// <summary>
    /// Reads watched segments.
    /// </summary>
    /// <param name="since">Optional lower timestamp bound.</param>
    /// <param name="limit">Optional result limit.</param>
    /// <returns>Watched segments.</returns>
    public IReadOnlyList<WatchedSegment> ReadSegments(DateTimeOffset? since = null, int? limit = null)
    {
        return ReadJsonLines<WatchedSegment>(_segmentsPath, since, limit, item => item.EndedAt);
    }

    private void AppendJsonLine<T>(string path, T value)
    {
        var line = JsonSerializer.Serialize(value, JsonOptions);
        lock (_fileLock)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }

    private IReadOnlyList<T> ReadJsonLines<T>(string path, DateTimeOffset? since, int? limit, Func<T, DateTimeOffset> timestampSelector)
    {
        if (!File.Exists(path))
        {
            return Array.Empty<T>();
        }

        List<T> results = [];
        string[] lines;
        lock (_fileLock)
        {
            lines = File.ReadAllLines(path);
        }

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var item = JsonSerializer.Deserialize<T>(line, JsonOptions);
                if (item is null)
                {
                    continue;
                }

                if (since.HasValue && timestampSelector(item) < since.Value)
                {
                    continue;
                }

                results.Add(item);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Skipping corrupt Playback Trace JSONL row in {Path}", path);
            }
        }

        IEnumerable<T> ordered = results.OrderByDescending(timestampSelector);
        if (limit.HasValue && limit.Value > 0)
        {
            ordered = ordered.Take(limit.Value);
        }

        return ordered.ToList();
    }
}
