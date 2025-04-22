using Domain.DTO;
using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IEventService
{
    Task<Event> CreateEventAsync(EventRequest newEvent);
    Task<Event> GetEventAsync(Guid eventId);
}