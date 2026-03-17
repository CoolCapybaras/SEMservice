using Microsoft.AspNetCore.SignalR;

namespace SEM.Services.Hubs;

public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public Task JoinEvent(Guid eventId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, eventId.ToString());
    }

    public Task LeaveEvent(Guid eventId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, eventId.ToString());
    }
}