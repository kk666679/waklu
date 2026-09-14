using Microsoft.AspNetCore.SignalR;

namespace HalalChain.Realtime;

public class NotificationHub : Hub
{
    public static class Events
    {
        public const string Receive = "notification.received";
    }

    public static new class Groups
    {
        public const string Prefix = "role:";
        public const string Admin = Prefix + "Admin";
        public const string Vendor = Prefix + "Vendor";
        public const string Customer = Prefix + "Customer";

        public static string RoleGroup(string role) => Prefix + role;
    }

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst("role")?.Value;
        if (string.IsNullOrEmpty(role))
        {
            if (Context.User?.IsInRole("Admin") == true) role = "Admin";
            else if (Context.User?.IsInRole("Vendor") == true) role = "Vendor";
            else if (Context.User?.IsInRole("Customer") == true) role = "Customer";
            else role = "Guest";
        }

        if (!string.IsNullOrEmpty(role))
        {
            await base.Groups.AddToGroupAsync(Context.ConnectionId, Groups.RoleGroup(role));
        }

        await base.OnConnectedAsync();
    }

    public Task Ping() => Clients.Caller.SendAsync("pong", DateTimeOffset.UtcNow);
}
