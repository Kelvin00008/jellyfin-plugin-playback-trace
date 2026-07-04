using System.Globalization;
using System.Net;
using System.Text;
using Jellyfin.Plugin.PlaybackTrace.Models;

namespace Jellyfin.Plugin.PlaybackTrace.Services;

internal static class HtmlExportBuilder
{
    public static string Build(IReadOnlyList<PlaybackTraceEvent> events, IReadOnlyList<WatchedSegment> segments, int days)
    {
        var seeks = events
            .Where(item => item.EventType is "seek_forward" or "seek_backward")
            .OrderByDescending(item => item.Timestamp)
            .ToList();

        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("<title>Playback Trace Export</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;margin:24px;color:#1f2933;background:#f7f8fa}");
        html.AppendLine("h1,h2{margin:0 0 12px} .meta{color:#5b6472;margin-bottom:24px}");
        html.AppendLine("table{width:100%;border-collapse:collapse;background:#fff;margin:0 0 28px;border:1px solid #d7dce2}");
        html.AppendLine("th,td{font-size:13px;text-align:left;padding:8px 10px;border-bottom:1px solid #e5e9ef;vertical-align:top}");
        html.AppendLine("th{background:#edf1f5;font-weight:650} tr:nth-child(even) td{background:#fbfcfd}");
        html.AppendLine(".num{font-variant-numeric:tabular-nums}.tag{display:inline-block;padding:2px 7px;border-radius:4px;background:#e8eef7}");
        html.AppendLine("</style></head><body>");
        html.AppendLine("<h1>Playback Trace Export</h1>");
        html.Append(CultureInfo.InvariantCulture, $"<div class=\"meta\">Generated {WebUtility.HtmlEncode(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture))}; range: last {days} days.</div>");

        AppendSegments(html, segments);
        AppendSeeks(html, seeks);
        AppendEvents(html, events);

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    public static string BuildSegmentsCsv(IReadOnlyList<WatchedSegment> segments)
    {
        var csv = new StringBuilder();
        csv.AppendLine("ended_at,user,item,client,device,timeline_start,timeline_end,watched_wall_seconds,closed_by,session_key");
        foreach (var segment in segments.OrderByDescending(item => item.EndedAt))
        {
            csv.AppendCsv(segment.EndedAt.ToString("O", CultureInfo.InvariantCulture));
            csv.AppendCsv(segment.UserName ?? segment.UserId ?? string.Empty);
            csv.AppendCsv(segment.ItemName ?? segment.ItemId ?? string.Empty);
            csv.AppendCsv(segment.ClientName ?? string.Empty);
            csv.AppendCsv(segment.DeviceName ?? string.Empty);
            csv.AppendCsv(FormatSeconds(segment.StartSeconds));
            csv.AppendCsv(FormatSeconds(segment.EndSeconds));
            csv.AppendCsv(segment.WatchedWallSeconds.ToString("0.###", CultureInfo.InvariantCulture));
            csv.AppendCsv(segment.ClosedBy);
            csv.AppendCsv(segment.SessionKey, endLine: true);
        }

        return csv.ToString();
    }

    private static void AppendSegments(StringBuilder html, IReadOnlyList<WatchedSegment> segments)
    {
        html.AppendLine("<h2>Watched Segments</h2>");
        html.AppendLine("<table><thead><tr><th>Ended</th><th>User</th><th>Item</th><th>Client</th><th>Device</th><th>Timeline</th><th>Watched</th><th>Closed By</th></tr></thead><tbody>");
        foreach (var segment in segments.OrderByDescending(item => item.EndedAt))
        {
            html.AppendLine("<tr>");
            html.AppendCell(segment.EndedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "num");
            html.AppendCell(segment.UserName ?? segment.UserId ?? string.Empty);
            html.AppendCell(segment.ItemName ?? segment.ItemId ?? string.Empty);
            html.AppendCell(segment.ClientName ?? string.Empty);
            html.AppendCell(segment.DeviceName ?? string.Empty);
            html.AppendCell($"{FormatSeconds(segment.StartSeconds)} - {FormatSeconds(segment.EndSeconds)}", "num");
            html.AppendCell(FormatDuration(segment.WatchedWallSeconds), "num");
            html.AppendCell(segment.ClosedBy);
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody></table>");
    }

    private static void AppendSeeks(StringBuilder html, IReadOnlyList<PlaybackTraceEvent> seeks)
    {
        html.AppendLine("<h2>Seek Events</h2>");
        html.AppendLine("<table><thead><tr><th>Time</th><th>User</th><th>Item</th><th>Type</th><th>From</th><th>To</th><th>Jump</th><th>Device</th></tr></thead><tbody>");
        foreach (var item in seeks)
        {
            var jump = (item.ToSeconds ?? item.PositionSeconds) - (item.FromSeconds ?? item.PositionSeconds);
            html.AppendLine("<tr>");
            html.AppendCell(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "num");
            html.AppendCell(item.UserName ?? item.UserId ?? string.Empty);
            html.AppendCell(item.ItemName ?? item.ItemId ?? string.Empty);
            html.AppendCell(item.EventType);
            html.AppendCell(FormatSeconds(item.FromSeconds ?? 0), "num");
            html.AppendCell(FormatSeconds(item.ToSeconds ?? item.PositionSeconds), "num");
            html.AppendCell(FormatSignedDuration(jump), "num");
            html.AppendCell(item.DeviceName ?? item.DeviceId ?? string.Empty);
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody></table>");
    }

    private static void AppendEvents(StringBuilder html, IReadOnlyList<PlaybackTraceEvent> events)
    {
        html.AppendLine("<h2>Raw Events</h2>");
        html.AppendLine("<table><thead><tr><th>Time</th><th>User</th><th>Item</th><th>Event</th><th>Position</th><th>Paused</th><th>Client</th><th>Device</th></tr></thead><tbody>");
        foreach (var item in events.OrderByDescending(row => row.Timestamp))
        {
            html.AppendLine("<tr>");
            html.AppendCell(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "num");
            html.AppendCell(item.UserName ?? item.UserId ?? string.Empty);
            html.AppendCell(item.ItemName ?? item.ItemId ?? string.Empty);
            html.AppendCell(item.EventType);
            html.AppendCell(FormatSeconds(item.PositionSeconds), "num");
            html.AppendCell(item.IsPaused ? "yes" : "no");
            html.AppendCell(item.ClientName ?? string.Empty);
            html.AppendCell(item.DeviceName ?? item.DeviceId ?? string.Empty);
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody></table>");
    }

    private static void AppendCell(this StringBuilder html, string value, string? cssClass = null)
    {
        var classAttribute = string.IsNullOrWhiteSpace(cssClass) ? string.Empty : $" class=\"{cssClass}\"";
        html.Append("<td");
        html.Append(classAttribute);
        html.Append('>');
        html.Append(WebUtility.HtmlEncode(value));
        html.AppendLine("</td>");
    }

    private static void AppendCsv(this StringBuilder csv, string value, bool endLine = false)
    {
        csv.Append('"');
        csv.Append(value.Replace("\"", "\"\"", StringComparison.Ordinal));
        csv.Append('"');
        csv.Append(endLine ? Environment.NewLine : ",");
    }

    private static string FormatSeconds(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return time.TotalHours >= 1
            ? string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}", (int)time.TotalHours, time.Minutes, time.Seconds)
            : string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}", time.Minutes, time.Seconds);
    }

    private static string FormatDuration(double seconds)
    {
        return FormatSeconds(seconds);
    }

    private static string FormatSignedDuration(double seconds)
    {
        var sign = seconds >= 0 ? "+" : "-";
        return sign + FormatSeconds(Math.Abs(seconds));
    }
}

