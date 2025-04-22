using Domain.DTO;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Services;

public class EventService: IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ApplicationDbContext _context;

    public EventService(IEventRepository eventRepository, ApplicationDbContext context)
    {
        _eventRepository = eventRepository;
        _context = context;
    }
    
    public async Task<Event> CreateEventAsync(EventRequest request)
    {
        var newEvent = new Event
        {
            Name = request.newEvent.Name,
            Description = request.newEvent.Description,
            StartDate = request.newEvent.StartDate,
            EndDate = request.newEvent.EndDate,
            Location = request.newEvent.Location,
            Format = request.newEvent.Format,
            EventType = request.newEvent.EventType,
            ResponsiblePerson = request.newEvent.ResponsiblePerson,
            MaxParticipants = request.newEvent.MaxParticipants,
            EventCategories = request.newEvent.EventCategories
        };

        newEvent = await _eventRepository.AddEventAsync(newEvent);

        foreach (var categoryId in request.newCategorys.)
        {
            
        }

    }

    public async Task<Event> GetEventAsync(Guid eventId)
    {
        return await _eventRepository.GetEventByIdAsync(eventId);
    }
}