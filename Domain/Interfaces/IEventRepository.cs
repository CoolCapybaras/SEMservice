using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IEventRepository
{
    Task<Event> AddEventAsync(Event neEvent);
    Task<IEnumerable<Event>> GetAllEventsAsync();
    Task<Event> GetEventByIdAsync(Guid eventId);
}