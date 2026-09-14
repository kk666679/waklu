namespace HalalChain.Realtime;

public enum RealtimeNotificationKind
{
    Info,
    Success,
    Warning,
    Error
}

public record RealtimeNotification(
    string Title,
    string Message,
    RealtimeNotificationKind Kind = RealtimeNotificationKind.Info,
    string? Topic = null,
    DateTimeOffset Timestamp = default);

public record ChatEnvelope(
    string ConversationId,
    string FromUserId,
    string FromDisplayName,
    string Text,
    DateTimeOffset SentAt);
