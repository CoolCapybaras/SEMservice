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
    Task AddSuscriberAsync(Guid eventId, Guid userId);
    Task DeleteSuscriber(Guid eventId, Guid userId);
    Task<EventRole> AddRoleToUser(Guid eventId, Guid userId,string roleName);
    Task<List<RolesResponse>> GetRolesByEvent(Guid eventId);
    public Task<List<EventUserResponse>> GetAllSuscribersAsync(Guid eventId);
    Task<Event> UpdateEventAsync(Guid eventId, Event updateModel);
    Task<List<string>> GetEventPhotosAsync(Guid eventId);
    Task AddEventPhotoAsync(Guid eventId, string filePath);
    Task AddContact(Guid eventId, Guid userId);
    Task<List<ContactResponse>> GetContacts(Guid eventId);
    Task<Roles> GetRoleByName(string roleName);
    Task<List<User>> Get10UsersByName(string userName);
}