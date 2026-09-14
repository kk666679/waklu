using HalalChain.Marketplace.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace HalalChain.Marketplace.Realtime;

public sealed class NotificationHub : Hub
{
    public async Task Broadcast(string title, string body, string style) => await Clients.All.SendCoreAsync("notify", new object?[] { title, body, style });
}

public sealed class RealtimeNotificationBridge : ComponentBase, IAsyncDisposable
{
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private NotificationState State { get; set; } = default!;

    private Microsoft.AspNetCore.SignalR.Client.HubConnection? _hub;
    protected override async Task OnInitializedAsync()
    {
        _hub = new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder()
            .WithUrl(Nav.ToAbsoluteUri("/hubs/notifications"))
            .Build();
        _hub.On<string, string, string>("notify", (title, body, style) =>
        {
            State.Push(title, body, style);
            InvokeAsync(StateHasChanged);
        });
        try { await _hub.StartAsync(); } catch { /* realtime degrades gracefully */ }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null) await _hub.DisposeAsync();
    }
}
