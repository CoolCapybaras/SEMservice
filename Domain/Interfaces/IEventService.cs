using Domain.DTO;
using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IEventService
{
    Task<Event> CreateEventAsync(EventRequest newEvent);
    Task<Event> GetEventAsync(Guid eventId);
    Task<List<Event>> SerchEventsAsync(SearchRequest request, int offset, int count);
    Task<List<Category>> GetAllCategoriesAsync();
    Task DeletEvent(Guid eventId);
}