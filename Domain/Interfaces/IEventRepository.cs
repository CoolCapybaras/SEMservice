using Domain.DTO;
using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IEventRepository
{
    Task<Event> AddEventAsync(EventRequest request);
    Task<IEnumerable<Event>> GetAllEventsAsync();
    Task<Event?> GetEventByIdAsync(Guid eventId);
    Task<List<Event>> SearchEventsAsync(SearchRequest request);
    Task<List<Event>> SearchArchivedEventsAsync(SearchRequest request, Guid userId);
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
    Task<EventRole> SetParticipantRoleForUser(Guid eventId, Guid userId, ParticipantRoleKind role);
    Task<List<EventFixedRoleInfoDto>> GetFixedRolesForEventAsync(Guid eventId, int count, int offset);
    Task<EventUserAndCountResponse> GetAllSuscribersAsync(Guid eventId, string? name, string? roleFil, int count, int offset);
    Task<List<EventUserResponse>> GetAllSuscribersWithoutOffsetAsync(Guid eventId, string? name, string? roleFil);
    Task<Event> UpdateEventAsync(Event @event);
    Task ReplaceEventTypesAsync(Guid eventId, IReadOnlyList<EventTypeKind> types);
    Task<List<PhotoResponse>> GetEventPhotosAsync(Guid eventId, int count, int offset);
    Task AddEventPhotoAsync(EventPhoto photo);
    Task AddContact(Guid eventId, Guid userId);
    Task<List<ContactResponse>> GetContacts(Guid eventId);
    Task<Event> FinishEventAsync(Event @event);
    Task<string> GetEventPhotoByIdAsync(Guid eventId, Guid photoId);
    Task<EventPhoto?> GetEventPhotoEntityAsync(Guid eventId, Guid photoId);
    Task<List<EventPhoto>> GetEventPhotoEntitiesAsync(Guid eventId, IReadOnlyCollection<Guid> photoIds);
    Task DeleteEventPhotoAsync(Guid eventId, Guid photoId);
    Task DeleteContact(Guid eventId, Guid userId);
    Task DeleteEventPhotosAsync(Guid eventId, List<Guid> photoIds);
    Task<Event> UpdateAvatarEventAsync(Event entity);
    Task<Event> CloneArchivedEventAsTemplateAsync(Event sourceEvent, Guid newOwnerId, string newName);
    Task<List<Guid>> GetPublicationSubscribersAsync(Guid organizerId, IReadOnlyCollection<string> categoryNames, Guid eventId);
}
