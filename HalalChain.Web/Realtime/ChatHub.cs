using Microsoft.AspNetCore.SignalR;

namespace HalalChain.Realtime;

public class ChatHub : Hub
{
    public static class Events
    {
        public const string Receive = "chat.received";
        public const string Typing = "chat.typing";
    }

    public async Task JoinConversation(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public async Task LeaveConversation(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public Task Typing(string conversationId) =>
        Clients.OthersInGroup(ConversationGroup(conversationId))
            .SendAsync(Events.Typing, new
            {
                ConversationId = conversationId,
                UserId = Context.UserIdentifier,
                At = DateTimeOffset.UtcNow
            });

    public static string ConversationGroup(string conversationId) => $"conv:{conversationId}";
}
