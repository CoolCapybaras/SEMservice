using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IChatRepository
{
    Task<List<EventChatMessage>> GetMessagesAsync(Guid eventId, int count, int offset);
    Task AddMessageAsync(EventChatMessage message);
}