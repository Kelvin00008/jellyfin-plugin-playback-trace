# Jellyfin Playback Trace

Jellyfin Playback Trace is a server-side Jellyfin plugin that records playback progress events and infers useful watch analytics without modifying Jellyfin clients.

It tracks:

- playback start, progress, pause, resume, seek forward, seek backward, and stop
- user, item, device, client, wall-clock time, and media timeline position
- watched timeline segments, such as `00:02:10-00:15:30`
- HTML export tables for segments, seek events, and raw events

## Important Limits

This plugin does not modify Jellyfin clients. It infers seeks and watched segments from the progress events that clients already report to Jellyfin. It can identify timeline jumps, skipped ranges, and rewatched ranges, but it cannot prove which exact button the viewer pressed.

## Install From GitHub Repository

After the first GitHub release is created, add this repository URL in Jellyfin:

```text
Dashboard -> Plugins -> Repositories -> Add
```

Manifest URL:

```text
https://raw.githubusercontent.com/Kelvin00008/jellyfin-plugin-playback-trace/main/manifest.json
```

Then install **Playback Trace** from the plugin catalog and restart Jellyfin.

## Manual Install

Build the plugin:

```bash
dotnet publish Jellyfin.Plugin.PlaybackTrace/Jellyfin.Plugin.PlaybackTrace.csproj -c Release -o dist/PlaybackTrace
```

Copy the plugin folder to Jellyfin:

```text
/config/plugins/PlaybackTrace/
```

For many Docker installs, `/config` is the Jellyfin config volume inside the container. On your NAS, use the host folder that is mapped to the container's `/config`.

Restart Jellyfin after copying.

## Export HTML

Open Jellyfin as an administrator:

```text
Dashboard -> Plugins -> Playback Trace
```

Use the HTML export button, or open:

```text
http://your-jellyfin-server:8096/playback_trace/export.html?days=30
```

Other endpoints:

```text
/playback_trace/events?limit=500
/playback_trace/segments?limit=500
/playback_trace/export.csv?days=30
```

## Data Storage

The plugin stores append-only JSONL files under Jellyfin's data path:

```text
playbacktrace/trace-events.jsonl
playbacktrace/watched-segments.jsonl
```

This keeps the plugin simple and avoids native database dependencies on NAS systems.

## Release

Create a tag to trigger the GitHub Actions release workflow:

```bash
git tag v0.1.0
git push origin v0.1.0
```

The repository already includes prebuilt zip files for manual install through `manifest.json`. Version `0.1.1.0` targets Jellyfin `10.10.3.0`; version `0.1.0.0` targets Jellyfin `10.11.0.0`. The workflow can also build a GitHub release later; if you use the workflow, it will recompute the checksum and update `manifest.json`.

## Privacy

Playback Trace records user viewing behavior. Tell viewers before enabling it, and restrict the export endpoints to administrators.
