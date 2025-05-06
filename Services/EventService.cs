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
        return await _eventRepository.CreateEventAsync(request);
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
}