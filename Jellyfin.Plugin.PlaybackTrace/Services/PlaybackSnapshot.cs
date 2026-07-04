using System.Collections;
using System.Reflection;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;

namespace Jellyfin.Plugin.PlaybackTrace.Services;

internal sealed class PlaybackSnapshot
{
    private const double TicksPerSecond = TimeSpan.TicksPerSecond;

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public string SessionKey { get; init; } = string.Empty;

    public string? SessionId { get; init; }

    public string? PlaySessionId { get; init; }

    public string? UserId { get; init; }

    public string? UserName { get; init; }

    public string? ItemId { get; init; }

    public string? ItemName { get; init; }

    public string? ItemType { get; init; }

    public string? ClientName { get; init; }

    public string? DeviceId { get; init; }

    public string? DeviceName { get; init; }

    public double PositionSeconds { get; init; }

    public bool IsPaused { get; init; }

    public bool IsAutomated { get; init; }

    public static PlaybackSnapshot? FromEvent(PlaybackProgressEventArgs args)
    {
        if (args.Item is null)
        {
            return null;
        }

        var session = GetProperty<SessionInfo>(args, "Session");
        var item = args.Item;
        var itemId = item.Id.Equals(Guid.Empty) ? null : item.Id.ToString("N");
        var userId = GetUserId(args, session);
        var userName = GetUserName(args, session);
        var sessionId = session?.Id;
        var playSessionId = EmptyToNull(args.PlaySessionId);
        var deviceId = EmptyToNull(args.DeviceId ?? session?.DeviceId);
        var sessionKey = BuildSessionKey(playSessionId, sessionId, userId, itemId, deviceId);

        return new PlaybackSnapshot
        {
            SessionKey = sessionKey,
            SessionId = sessionId,
            PlaySessionId = playSessionId,
            UserId = userId,
            UserName = userName,
            ItemId = itemId,
            ItemName = GetItemName(item),
            ItemType = args.MediaInfo?.Type.ToString() ?? item.GetType().Name,
            ClientName = EmptyToNull(args.ClientName ?? session?.Client),
            DeviceId = deviceId,
            DeviceName = EmptyToNull(args.DeviceName ?? session?.DeviceName),
            PositionSeconds = TicksToSeconds(args.PlaybackPositionTicks),
            IsPaused = args.IsPaused,
            IsAutomated = GetProperty<bool>(args, "IsAutomated")
        };
    }

    private static string BuildSessionKey(string? playSessionId, string? sessionId, string? userId, string? itemId, string? deviceId)
    {
        if (!string.IsNullOrWhiteSpace(playSessionId))
        {
            return $"play:{playSessionId}";
        }

        return $"session:{sessionId ?? "unknown"}|user:{userId ?? "unknown"}|item:{itemId ?? "unknown"}|device:{deviceId ?? "unknown"}";
    }

    private static string? GetUserId(PlaybackProgressEventArgs args, SessionInfo? session)
    {
        if (session is not null && session.UserId != Guid.Empty)
        {
            return session.UserId.ToString("N");
        }

        var user = GetFirstUser(args);
        var id = GetProperty<Guid>(user, "Id");
        return id != Guid.Empty ? id.ToString("N") : null;
    }

    private static string? GetUserName(PlaybackProgressEventArgs args, SessionInfo? session)
    {
        if (!string.IsNullOrWhiteSpace(session?.UserName))
        {
            return session.UserName;
        }

        var user = GetFirstUser(args);
        return EmptyToNull(GetProperty<string>(user, "Username") ?? GetProperty<string>(user, "Name"));
    }

    private static object? GetFirstUser(PlaybackProgressEventArgs args)
    {
        var users = GetProperty<IEnumerable>(args, "Users");
        if (users is null)
        {
            return null;
        }

        foreach (var user in users)
        {
            return user;
        }

        return null;
    }

    private static T? GetProperty<T>(object? source, string propertyName)
    {
        if (source is null)
        {
            return default;
        }

        var property = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null)
        {
            return default;
        }

        var value = property.GetValue(source);
        if (value is T typed)
        {
            return typed;
        }

        return default;
    }

    private static double TicksToSeconds(long? ticks)
    {
        return Math.Max(0, (ticks ?? 0) / TicksPerSecond);
    }

    private static string GetItemName(BaseItem item)
    {
        return string.IsNullOrWhiteSpace(item.Name) ? "Unknown item" : item.Name;
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
