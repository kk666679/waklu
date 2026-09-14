namespace HalalChain.Realtime;

public interface IRealtimeBroadcaster
{
    Task BroadcastNotificationAsync(RealtimeNotification notification, CancellationToken ct = default);
    Task SendNotificationToUserAsync(string userId, RealtimeNotification notification, CancellationToken ct = default);
    Task SendNotificationToRoleAsync(string role, RealtimeNotification notification, CancellationToken ct = default);
    Task BroadcastChatAsync(ChatEnvelope message, CancellationToken ct = default);
    Task SendChatToUserAsync(string userId, ChatEnvelope message, CancellationToken ct = default);
}
