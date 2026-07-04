using Jellyfin.Plugin.PlaybackTrace.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.PlaybackTrace;

/// <summary>
/// Registers Playback Trace services with Jellyfin.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<PlaybackTraceStore>();
        serviceCollection.AddHostedService<EventMonitorEntryPoint>();
    }
}

