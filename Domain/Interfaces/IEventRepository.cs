﻿using Domain.DTO;
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
    Task AddSuscriberAsync(Guid eventId, Guid userId);
    Task DeleteSuscriber(Guid eventId, Guid userId);
    Task<EventRole> AddRoleToUser(Guid eventId, Guid userId,string roleName);
    Task<List<RolesResponse>> GetRolesByEvent(Guid eventId);
    Task<List<EventUserResponse>> GetAllSuscribersAsync(Guid eventId);
    Task<Event> UpdateEventAsync(Event @event);
    Task<List<string>> GetEventPhotosAsync(Guid eventId);
    Task AddEventPhotoAsync(EventPhoto photo);
    Task AddContact(Guid eventId, Guid userId);
    Task<List<ContactResponse>> GetContacts(Guid eventId);
    Task<Roles> GetRoleByName(string roleName);
    Task<List<User>> Get10UsersByName(string userName);
}