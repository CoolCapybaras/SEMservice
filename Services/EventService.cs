using Domain.DTO;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;

    public EventService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<Event> CreateEventAsync(EventRequest request)
    {
        return await _eventRepository.AddEventAsync(request);
    }

    public async Task<Event?> GetEventByIdAsync(Guid eventId)
    {
        return await _eventRepository.GetEventByIdAsync(eventId);
    }

    public async Task<List<Event>> SearchEventsAsync(SearchRequest request)
    {
        return await _eventRepository.SearchEventsAsync(request);
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _eventRepository.GetAllCategoriesAsync();
    }

    public async Task DeleteEventAsync(Guid eventId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        await _eventRepository.DeleteEventAndUnusedCategoriesAsync(_event);
    }

    public async Task AddSuscriberAsync(Guid eventId, Guid userId)
    {
        await _eventRepository.AddSuscriberAsync(eventId, userId);
    }

    public async Task DeleteSuscriber(Guid eventId, Guid userId)
    {
        await _eventRepository.DeleteSuscriber(eventId, userId);
    }

    public async Task<EventRole> AddRoleToUser(Guid eventId, Guid userId,string roleName)
    {
        return await _eventRepository.AddRoleToUser(eventId, userId, roleName);
    }

    public async Task<List<RolesResponse>> GetRolesByEvent(Guid eventId)
    {
        return await _eventRepository.GetRolesByEvent(eventId);
    }

    public async Task<List<EventUserResponse>> GetAllSuscribersAsync(Guid eventId)
    {
        return await _eventRepository.GetAllSuscribersAsync(eventId);
    }

    public async Task<Event> UpdateEventAsync(Guid eventId, Event updateModel)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        
        curEvent.Name = updateModel.Name ?? curEvent.Name;
        curEvent.Description = updateModel.Description ?? curEvent.Description;
        curEvent.StartDate = updateModel.StartDate ?? curEvent.StartDate;
        curEvent.EndDate = updateModel.EndDate ?? curEvent.EndDate;
        curEvent.Location = updateModel.Location ?? curEvent.Location;
        curEvent.Format = updateModel.Format ?? curEvent.Format;
        curEvent.EventType = updateModel.EventType ?? curEvent.EventType;
        curEvent.MaxParticipants = updateModel.MaxParticipants ?? curEvent.MaxParticipants;

        var updatedEvent = await _eventRepository.UpdateEventAsync(curEvent);

        return updatedEvent;
    }

    public async Task<List<string>> GetEventPhotosAsync(Guid eventId)
    {
        return await _eventRepository.GetEventPhotosAsync(eventId);
    }
    
    public async Task AddEventPhotoAsync(Guid eventId, string filePath)
    {
        var photo = new EventPhoto
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            FilePath = filePath
        };

        await _eventRepository.AddEventPhotoAsync(photo);
    }

    public async Task AddContact(Guid eventId, Guid userId)
    {
        await _eventRepository.AddContact(eventId, userId);
    }

    public async Task<List<ContactResponse>> GetContacts(Guid eventId)
    {
        return await _eventRepository.GetContacts(eventId);
    }

    public async Task<Roles> GetRoleByName(string roleName)
    {
        return await _eventRepository.GetRoleByName(roleName);
    }

    public async Task<List<User>> Get10UsersByName(string userName)
    {
        return await _eventRepository.Get10UsersByName(userName);
    }
}