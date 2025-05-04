using Domain.DTO;
using Microsoft.EntityFrameworkCore;
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
    
    public async Task<Event> CreateEventAsync(EventRequest newEvent)
    {
        List<Guid> categories = new List<Guid>();
        var existingCategories = await _context.Categories
            .Where(c => newEvent.Categories.Contains(c.Name))
            .ToListAsync();
        var newCategoryNames = newEvent.Categories
            .Except(existingCategories.Select(c => c.Name))
            .ToList();
        foreach (var category in newCategoryNames)
        {
            var newCategory = new Category
            {
                Name = category
            };
            await _eventRepository.AddCategoryAsync(newCategory);
            categories.Add(newCategory.id);
        }   
        
        var neEvent = new Event
        {
            Name = newEvent.Name,
            Description = newEvent.Description,
            StartDate = newEvent.StartDate,
            EndDate = newEvent.EndDate,
            Location = newEvent.Location,
            Format = newEvent.Format,
            EventType = newEvent.EventType,
            ResponsiblePerson = newEvent.ResponsiblePerson,
            MaxParticipants = newEvent.MaxParticipants,
        };

       await _eventRepository.AddEventAsync(neEvent);
       var newEventId = neEvent.Id;

        foreach (var categoryId in categories)
        {
            await _eventRepository.AddEventCategoryConnAsync(newEventId, categoryId);
        }

        return neEvent;

    }

    public async Task<Event> GetEventAsync(Guid eventId)
    {
        return await _eventRepository.GetEventByIdAsync(eventId);
    }

    public async Task<List<Event>> SerchEventsAsync(SearchRequest request, int offset, int count)
    {
        return await _eventRepository.SearchEvents(request.Start, request.End, request.Name, request.Categories,
            request.Organizators, request.Format, request.isFreePlaces, offset, count);
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _eventRepository.GetAllCategoriesAsync();
    }

    public async Task DeletEvent(Guid eventId)
    {
        var @event = await _eventRepository.GetEventByIdAsync(eventId);
        var eventToDelete = await _context.Events
            .Include(e => e.EventCategories)
            .FirstOrDefaultAsync(e => e.Id == @event.Id);
        if (eventToDelete != null)
           await _eventRepository.DeleteEventAndUnusedCategoriesAsync(eventToDelete);
    }
}