using Microsoft.AspNetCore.SignalR;

namespace HalalChain.Realtime;

public class SignalRRealtimeBroadcaster : IRealtimeBroadcaster
{
    private readonly IHubContext<NotificationHub> _notifications;
    private readonly IHubContext<ChatHub> _chat;

    public SignalRRealtimeBroadcaster(IHubContext<NotificationHub> notifications, IHubContext<ChatHub> chat)
    {
        _notifications = notifications;
        _chat = chat;
    }

    public Task BroadcastNotificationAsync(RealtimeNotification notification, CancellationToken ct = default)
        => _notifications.Clients.All.SendAsync(NotificationHub.Events.Receive, notification, ct);

    public Task SendNotificationToUserAsync(string userId, RealtimeNotification notification, CancellationToken ct = default)
        => _notifications.Clients.User(userId).SendAsync(NotificationHub.Events.Receive, notification, ct);

    public Task SendNotificationToRoleAsync(string role, RealtimeNotification notification, CancellationToken ct = default)
        => _notifications.Clients.Group(NotificationHub.Groups.RoleGroup(role)).SendAsync(NotificationHub.Events.Receive, notification, ct);

    public Task BroadcastChatAsync(ChatEnvelope message, CancellationToken ct = default)
        => _chat.Clients.All.SendAsync(ChatHub.Events.Receive, message, ct);

    public Task SendChatToUserAsync(string userId, ChatEnvelope message, CancellationToken ct = default)
        => _chat.Clients.User(userId).SendAsync(ChatHub.Events.Receive, message, ct);
}
