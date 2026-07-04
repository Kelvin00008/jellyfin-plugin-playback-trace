using System.Globalization;
using System.Net.Mime;
using Jellyfin.Plugin.PlaybackTrace.Services;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.PlaybackTrace.Api;

/// <summary>
/// Playback Trace export API.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("playback_trace")]
public sealed class PlaybackTraceController : ControllerBase
{
    private readonly PlaybackTraceStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackTraceController"/> class.
    /// </summary>
    /// <param name="store">Playback Trace store.</param>
    public PlaybackTraceController(PlaybackTraceStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Gets recent raw events.
    /// </summary>
    /// <param name="limit">Maximum number of rows.</param>
    /// <returns>Raw events.</returns>
    [HttpGet("events")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetEvents([FromQuery] int limit = 500)
    {
        return Ok(_store.ReadEvents(limit: ClampLimit(limit)));
    }

    /// <summary>
    /// Gets recent watched segments.
    /// </summary>
    /// <param name="limit">Maximum number of rows.</param>
    /// <returns>Watched segments.</returns>
    [HttpGet("segments")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetSegments([FromQuery] int limit = 500)
    {
        return Ok(_store.ReadSegments(limit: ClampLimit(limit)));
    }

    /// <summary>
    /// Exports records as an HTML table report.
    /// </summary>
    /// <param name="days">Number of days to include.</param>
    /// <returns>HTML report.</returns>
    [HttpGet("export.html")]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ContentResult ExportHtml([FromQuery] int days = 30)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-ClampDays(days));
        var events = _store.ReadEvents(since, 10000);
        var segments = _store.ReadSegments(since, 10000);
        var html = HtmlExportBuilder.Build(events, segments, ClampDays(days));
        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>
    /// Exports watched segments as CSV.
    /// </summary>
    /// <param name="days">Number of days to include.</param>
    /// <returns>CSV report.</returns>
    [HttpGet("export.csv")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public FileContentResult ExportCsv([FromQuery] int days = 30)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-ClampDays(days));
        var segments = _store.ReadSegments(since, 10000);
        var csv = HtmlExportBuilder.BuildSegmentsCsv(segments);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        var fileName = string.Format(CultureInfo.InvariantCulture, "playback-trace-{0:yyyyMMdd-HHmmss}.csv", DateTime.UtcNow);
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private static int ClampDays(int days)
    {
        return Math.Clamp(days, 1, 3650);
    }

    private static int ClampLimit(int limit)
    {
        return Math.Clamp(limit, 1, 50000);
    }
}

