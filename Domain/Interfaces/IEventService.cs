using Domain;
using Domain.DTO;
using Microsoft.AspNetCore.Http;
using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IEventService
{
    Task<ServiceResult<Event>> CreateEventAsync(EventRequest newEvent, Guid modId);
    Task<ServiceResult<EventResponse>> GetEventByIdAsync(Guid eventId, Guid currentUserId);
    Task<ServiceResult<List<Event>>> SearchEventsAsync(SearchRequest request, Guid currentUserId);
    Task<ServiceResult<List<Event>>> SearchArchivedEventsAsync(SearchRequest request, Guid userId);
    Task<ServiceResult<List<CategoryResponse>>> GetEventCategoriesAsync(Guid eventId);
    Task<ServiceResult<List<Event>>> GetMyEventsAsync(Guid userId);
    Task<ServiceResult<List<Category>>> GetAllCategoriesAsync();
    Task<ServiceResult<bool>> DeleteEventAsync(Guid eventId, Guid userId);
    Task<ServiceResult<bool>> AddSuscriberAsync(Guid eventId, Guid userId);
    Task<ServiceResult<bool>> DeleteSuscriber(Guid eventId, Guid userId, Guid? transferToUserId = null);
    Task<ServiceResult<bool>> DeleteByOrganizerSuscriber(Guid eventId, Guid userId, Guid organizerId);
    Task<ServiceResult<EventRole>> SetParticipantRoleForUserAsync(Guid eventId, Guid userId, ParticipantRoleKind role, Guid currentUserId);
    Task<ServiceResult<List<EventFixedRoleInfoDto>>> GetRolesByEvent(Guid eventId, int count, int offset);
    Task<ServiceResult<EventUserAndCountResponse>> GetAllSuscribersAsync(Guid eventId, string? name, string? role, int count, int offset);
    Task<ServiceResult<List<EventUserResponse>>> GetAllSuscribersWithoutOffsetAsync(Guid eventId, string? name, string? role);
    Task<ServiceResult<List<AssigneeCandidateDto>>> GetAssigneeCandidatesAsync(Guid eventId, Guid userId);
    Task<ServiceResult<Event>> UpdateEventAsync(Guid eventId, EventUpdateRequest request, Guid userId);
    Task<ServiceResult<Event>> UpdateEventLifecycleStateAsync(Guid eventId, EventLifecycleUpdateRequest request, Guid userId);
    Task<ServiceResult<Event>> SetEventCancellationAsync(Guid eventId, EventCancellationRequest request, Guid userId);
    Task<ServiceResult<List<PhotoResponse>>> GetEventPhotosAsync(Guid eventId, int count, int offset);
    Task<ServiceResult<string>> AddEventPhotoAsync(Guid eventId, IFormFile file);
    Task<ServiceResult<bool>> AddContact(Guid eventId, Guid userId, Guid currentUserId);
    Task<ServiceResult<List<ContactResponse>>> GetContacts(Guid eventId);
    Task<ServiceResult<EventCategory>> AddCategoryToEventAsync(Guid eventId, string categoryName, Guid userId);
    Task<ServiceResult<bool>> DeleteCategoryInEventAsync(Guid eventId, Guid categoryId, Guid userId);
    Task<ServiceResult<Event>> FinishEventAsync(Guid Eventid, Guid userId);
    Task<ServiceResult<string>> GetEventPhotoByIdAsync(Guid eventId, Guid photoId);
    Task<ServiceResult<DownloadMediaResult>> DownloadEventPhotoAsync(Guid eventId, Guid photoId);
    Task<ServiceResult<DownloadMediaResult>> DownloadEventPhotosArchiveAsync(Guid eventId, IReadOnlyCollection<Guid> photoIds);
    Task<ServiceResult<bool>> DeleteEventPhotoAsync(Guid eventId, Guid photoId, Guid userId);
    Task<ServiceResult<bool>> DeleteContact(Guid eventId, Guid userId, Guid currentUserId);
    Task<ServiceResult<bool>> DeleteEventPhotosAsync(Guid eventId, List<Guid> photoIds, Guid userId);
    Task<ServiceResult<string>> UploadEventAvatarAsync(Guid eventId, IFormFile avatar, Guid userId);
    Task<ServiceResult<Event>> CopyArchivedEventAsTemplateAsync(Guid sourceEventId, Guid userId, string? name);
}
