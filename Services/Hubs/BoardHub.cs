using Microsoft.AspNetCore.SignalR;

namespace SEM.Services.Hubs;

public class BoardHub : Hub
{
    public Task JoinEvent(Guid eventId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, eventId.ToString());
    }

    public Task LeaveEvent(Guid eventId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, eventId.ToString());
    }
}