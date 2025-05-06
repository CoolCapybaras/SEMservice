using Domain.DTO;
using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IEventRepository
{
    Task<Event> AddEventAsync(EventRequest request);
    Task<IEnumerable<Event>> GetAllEventsAsync();
    Task<Event?> GetEventByIdAsync(Guid eventId);
    Task<List<Event>> SearchEventsAsync(SearchRequest request);
    Task AddCategoryAsync(Category category);
    Task<List<Category>> GetAllCategoriesAsync();
    Task DeleteEventAndUnusedCategoriesAsync(Event eventToDelete);
    Task AddEventCategoryConnAsync(Guid newEventId, Guid categoryId);
}