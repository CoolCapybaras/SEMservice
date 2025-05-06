using Domain.DTO;
using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IEventService
{
    Task<Event> CreateEventAsync(EventRequest newEvent);
    Task<Event?> GetEventByIdAsync(Guid eventId);
    Task<List<Event>> SearchEventsAsync(SearchRequest request);
    Task<List<Category>> GetAllCategoriesAsync();
    Task DeleteEventAsync(Guid eventId);
}