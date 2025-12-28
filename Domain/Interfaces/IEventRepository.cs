using Domain.DTO;
using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IEventRepository
{
    Task<Event> AddEventAsync(EventRequest request);
    Task<IEnumerable<Event>> GetAllEventsAsync();
    Task<Event?> GetEventByIdAsync(Guid eventId);
    Task<List<Event>> SearchEventsAsync(SearchRequest request);
    Task<List<CategoryResponse>> GetEventCategoriesAsync(Guid eventId);
    Task<List<Event>> GetMyEventsAsync(Guid userId);
    Task<EventCategory> AddCategoryToEventAsync(Guid eventId, string categoryName);
    Task DeleteCategoryInEventAsync(Guid eventId, Guid categoryId);
    Task AddCategoryAsync(Category category);
    Task<List<Category>> GetAllCategoriesAsync();
    Task DeleteEventAndUnusedCategoriesAsync(Event eventToDelete);
    Task AddEventCategoryConnAsync(Guid newEventId, Guid categoryId);
    Task AddSuscriberAsync(Guid eventId, Guid userId);
    Task DeleteSuscriber(Guid eventId, Guid userId);
    Task DeleteSuscriberByOrganizer(Guid eventId, Guid userId);
    Task<EventRole> AddRoleToUser(Guid eventId, Guid userId, Guid roleId);
    Task<List<Roles>> GetRolesByEvent(Guid eventId, int count, int offset);
    Task<EventUserAndCountResponse> GetAllSuscribersAsync(Guid eventId, string? name, string? roleFil, int count, int offset);
    Task<List<EventUserResponse>> GetAllSuscribersWithoutOffsetAsync(Guid eventId, string? name, string? roleFil);
    Task<Event> UpdateEventAsync(Event @event);
    Task<List<PhotoResponse>> GetEventPhotosAsync(Guid eventId, int count, int offset);
    Task AddEventPhotoAsync(EventPhoto photo);
    Task AddContact(Guid eventId, Guid userId);
    Task<List<ContactResponse>> GetContacts(Guid eventId);
    Task<Roles> GetRoleByName(string roleName);
    Task<Event> FinishEventAsync(Event @event);
    Task<Roles> CreateRoleAsync(Roles role);
    Task<Roles> GetRoleByIdAsync(Guid eventId, Guid roleId);
    Task<Roles> UpdateRoleAsync(Roles role);
    Task DeleteRoleAsync(Guid eventId, Guid roleId);
    Task<string> GetEventPhotoByIdAsync(Guid eventId, Guid photoId);
    Task DeleteEventPhotoAsync(Guid eventId, Guid photoId);
    Task DeleteContact(Guid eventId, Guid userId);
    Task DeleteEventPhotosAsync(Guid eventId, List<Guid> photoIds);
    Task<Event> UpdateAvatarEventAsync(Event entity);
}