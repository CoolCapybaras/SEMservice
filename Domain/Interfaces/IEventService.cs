using Domain;
using Domain.DTO;
using Microsoft.AspNetCore.Http;
using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IEventService
{
    Task<ServiceResult<Event>> CreateEventAsync(EventRequest newEvent, Guid modId);
    Task<ServiceResult<Event>> GetEventByIdAsync(Guid eventId);
    Task<ServiceResult<List<Event>>> SearchEventsAsync(SearchRequest request);
    Task<ServiceResult<List<Category>>> GetAllCategoriesAsync();
    Task<ServiceResult<bool>> DeleteEventAsync(Guid eventId, Guid userId);
    Task<ServiceResult<bool>> AddSuscriberAsync(Guid eventId, Guid userId);
    Task<ServiceResult<bool>> DeleteSuscriber(Guid eventId, Guid userId);
    Task<ServiceResult<EventRole>> AddRoleToUser(Guid eventId, Guid userId, Guid roleId, Guid currentUserId);
    Task<ServiceResult<List<Roles>>> GetRolesByEvent(Guid eventId, int count, int offset);
    Task<ServiceResult<List<EventUserResponse>>> GetAllSuscribersAsync(Guid eventId, string? name, int count, int offset);
    Task<ServiceResult<List<EventUserResponse>>> GetAllSuscribersWithoutOffsetAsync(Guid eventId, string? name);
    Task<ServiceResult<Event>> UpdateEventAsync(Guid eventId, EventUpdateRequest request, Guid userId);
    Task<ServiceResult<List<string>>> GetEventPhotosAsync(Guid eventId, int count, int offset);
    Task<ServiceResult<string>> AddEventPhotoAsync(Guid eventId, IFormFile file);
    Task<ServiceResult<bool>> AddContact(Guid eventId, Guid userId, Guid currentUserId);
    Task<ServiceResult<List<ContactResponse>>> GetContacts(Guid eventId);
    Task<ServiceResult<Roles>> GetRoleByName(string roleName);
    Task<ServiceResult<Event>> FinishEventAsync(Guid Eventid, Guid userId);
    Task<ServiceResult<Roles>> CreateRoleAsync(string roleName, Guid eventId, Guid userId);
    Task<ServiceResult<Roles>> GetRoleByIdAsync(Guid eventId, Guid roleId);
    Task<ServiceResult<Roles>> UpdateRoleAsync(string newRoleName, Guid eventId, Guid roleId, Guid userId);
    Task<ServiceResult<bool>> DeleteRoleAsync(Guid eventId, Guid roleId, Guid userId);
    Task<ServiceResult<string>> GetEventPhotoByIdAsync(Guid eventId, Guid photoId);
    Task<ServiceResult<bool>> DeleteEventPhotoAsync(Guid eventId, Guid photoId, Guid userId);
    Task<ServiceResult<bool>> DeleteContact(Guid eventId, Guid userId, Guid currentUserId);
}