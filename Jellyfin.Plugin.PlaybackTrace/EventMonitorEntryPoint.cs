using System.Collections.Concurrent;
using Jellyfin.Plugin.PlaybackTrace.Models;
using Jellyfin.Plugin.PlaybackTrace.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PlaybackTrace;

/// <summary>
/// Monitors Jellyfin playback events.
/// </summary>
public sealed class EventMonitorEntryPoint : IHostedService, IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly PlaybackTraceStore _store;
    private readonly ILogger<EventMonitorEntryPoint> _logger;
    private readonly ConcurrentDictionary<string, TraceSessionState> _states = new(StringComparer.Ordinal);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventMonitorEntryPoint"/> class.
    /// </summary>
    /// <param name="sessionManager">Session manager.</param>
    /// <param name="store">Trace store.</param>
    /// <param name="logger">Logger.</param>
    public EventMonitorEntryPoint(
        ISessionManager sessionManager,
        PlaybackTraceStore store,
        ILogger<EventMonitorEntryPoint> logger)
    {
        _sessionManager = sessionManager;
        _store = store;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackProgress += OnPlaybackProgress;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _logger.LogInformation("Playback Trace event monitor started");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Unsubscribe();
        _logger.LogInformation("Playback Trace event monitor stopped");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Unsubscribe();
        _disposed = true;
    }

    private void OnPlaybackStart(object? sender, PlaybackProgressEventArgs args)
    {
        var snapshot = PlaybackSnapshot.FromEvent(args);
        if (snapshot is null)
        {
            return;
        }

        var configuration = Plugin.Instance?.Configuration ?? new();
        if (_states.TryRemove(snapshot.SessionKey, out var existing))
        {
            AppendClosedSegment(existing, snapshot, "replaced", configuration, snapshot.PositionSeconds);
        }

        var state = new TraceSessionState
        {
            Snapshot = snapshot,
            LastPositionSeconds = snapshot.PositionSeconds,
            LastWallTime = snapshot.Timestamp,
            IsPaused = snapshot.IsPaused,
            LastSampleAt = snapshot.Timestamp
        };

        if (!snapshot.IsPaused)
        {
            PlaybackTraceAnalyzer.OpenSegment(state, snapshot);
        }

        _states[snapshot.SessionKey] = state;
        _store.AppendEvent(PlaybackTraceAnalyzer.BuildEvent("start", snapshot));
    }

    private void OnPlaybackProgress(object? sender, PlaybackProgressEventArgs args)
    {
        var snapshot = PlaybackSnapshot.FromEvent(args);
        if (snapshot is null)
        {
            return;
        }

        var configuration = Plugin.Instance?.Configuration ?? new();
        if (!_states.TryGetValue(snapshot.SessionKey, out var state))
        {
            OnPlaybackStart(sender, args);
            return;
        }

        if (!state.IsPaused && snapshot.IsPaused)
        {
            AppendClosedSegment(state, snapshot, "pause", configuration, snapshot.PositionSeconds);
            _store.AppendEvent(PlaybackTraceAnalyzer.BuildEvent("pause", snapshot, state));
        }
        else if (state.IsPaused && !snapshot.IsPaused)
        {
            PlaybackTraceAnalyzer.OpenSegment(state, snapshot);
            _store.AppendEvent(PlaybackTraceAnalyzer.BuildEvent("resume", snapshot, state));
        }
        else if (!snapshot.IsPaused && PlaybackTraceAnalyzer.IsSeek(state, snapshot, configuration))
        {
            var seekType = snapshot.PositionSeconds >= state.LastPositionSeconds ? "seek_forward" : "seek_backward";
            AppendClosedSegment(state, snapshot, seekType, configuration);
            _store.AppendEvent(PlaybackTraceAnalyzer.BuildEvent(seekType, snapshot, state, state.LastPositionSeconds, snapshot.PositionSeconds));
            PlaybackTraceAnalyzer.OpenSegment(state, snapshot);
        }
        else if (!snapshot.IsPaused && ShouldWriteSample(state, snapshot, configuration))
        {
            _store.AppendEvent(PlaybackTraceAnalyzer.BuildEvent("normal_play", snapshot, state));
            state.LastSampleAt = snapshot.Timestamp;
        }

        UpdateState(state, snapshot);
    }

    private void OnPlaybackStopped(object? sender, PlaybackStopEventArgs args)
    {
        var snapshot = PlaybackSnapshot.FromEvent(args);
        if (snapshot is null)
        {
            return;
        }

        var configuration = Plugin.Instance?.Configuration ?? new();
        if (_states.TryRemove(snapshot.SessionKey, out var state))
        {
            AppendClosedSegment(state, snapshot, "stop", configuration, snapshot.PositionSeconds);
            _store.AppendEvent(PlaybackTraceAnalyzer.BuildEvent("stop", snapshot, state));
        }
        else
        {
            _store.AppendEvent(PlaybackTraceAnalyzer.BuildEvent("stop", snapshot));
        }
    }

    private static bool ShouldWriteSample(TraceSessionState state, PlaybackSnapshot snapshot, Configuration.PluginConfiguration configuration)
    {
        var interval = Math.Max(1, configuration.SampleIntervalSeconds);
        return (snapshot.Timestamp - state.LastSampleAt).TotalSeconds >= interval;
    }

    private void AppendClosedSegment(TraceSessionState state, PlaybackSnapshot snapshot, string closedBy, Configuration.PluginConfiguration configuration, double? endSeconds = null)
    {
        var segment = PlaybackTraceAnalyzer.CloseSegment(state, snapshot, closedBy, configuration, endSeconds);
        if (segment is not null)
        {
            _store.AppendSegment(segment);
        }
    }

    private static void UpdateState(TraceSessionState state, PlaybackSnapshot snapshot)
    {
        state.Snapshot = snapshot;
        state.LastPositionSeconds = snapshot.PositionSeconds;
        state.LastWallTime = snapshot.Timestamp;
        state.IsPaused = snapshot.IsPaused;
    }

    private void Unsubscribe()
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackProgress -= OnPlaybackProgress;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
    }
}
