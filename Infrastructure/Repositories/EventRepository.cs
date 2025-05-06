using Domain.DTO;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;

    public EventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Event> AddEventAsync(EventRequest request)
    {
        var existingCategories = await _context.Categories
            .Where(c => request.Categories.Contains(c.Name))
            .ToListAsync();
        var newCategories = request.Categories
            .Where(name => !existingCategories.Any(c => c.Name == name))
            .Select(name => new Category { Name = name })
            .ToList();

        var newEvent = new Event
        {
            Name = request.Name,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Location = request.Location,
            Format = request.Format,
            EventType = request.EventType,
            ResponsiblePersonId = request.ResponsiblePersonId,
            MaxParticipants = request.MaxParticipants ?? -1
        };

        _context.Categories.AddRange(newCategories);
        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        var eventCategories = existingCategories.Concat(newCategories)
            .Select(c => new EventCategory
            {
                EventId = newEvent.Id,
                CategoryId = c.Id
            })
            .ToList();

        _context.EventCategories.AddRange(eventCategories);
        await _context.SaveChangesAsync();

        return newEvent;
    }

    public async Task<IEnumerable<Event>> GetAllEventsAsync()
    {
        return await _context.Events.ToListAsync();
    }

    public async Task<Event?> GetEventByIdAsync(Guid eventId)
    {
        return await _context.Events
            .Include(e => e.EventCategories)
            .ThenInclude(ec => ec.Category)
            .FirstOrDefaultAsync(e => e.Id == eventId);
    }

    public async Task<List<Event>> SearchEventsAsync(SearchRequest request)
    {
        var query = _context.Events.AsQueryable();

        // Фильтр по временному промежутку
        if (request.Start != null && request.End != null)
        {
            query = query.Where(e => e.StartDate >= request.Start && e.EndDate <= request.End);
        }

        // Фильтр по имени
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            query = query.Where(e => EF.Functions.ILike(e.Name, $"{request.Name}%"));
        }

        // Фильтр по организаторам
        if (request.Organizators != null && request.Organizators.Count > 0)
        {
            query = query.Where(e => request.Organizators.Contains(e.ResponsiblePersonId));
        }

        // Фильтр по формату
        if (!string.IsNullOrWhiteSpace(request.Format))
        {
            query = query.Where(e => e.Format == request.Format);
        }

        // Фильтр по свободным местам
        if (request.HasFreePlaces == true)
        {
            query = query.Where(e => e.Users.Count < e.MaxParticipants);
        }

        // Фильтр по категориям
        if (request.Categories != null && request.Categories.Count > 0)
        {
            query = query.Where(e => e.EventCategories.Any(c => request.Categories.Contains(c.Event.Name)));
        }

        query = query
            .OrderBy(e => e.StartDate)
            .Skip(request.Offset)
            .Take(request.Count);

        return await query
            .Include(e => e.EventCategories)
            .ThenInclude(ec => ec.Category)
            .ToListAsync();
    }

    public async Task AddCategoryAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories.ToListAsync();
    }

    public async Task DeleteEventAndUnusedCategoriesAsync(Event eventToDelete)
    {
        var eventCategories = await _context.EventCategories
            .Where(ec => ec.EventId == eventToDelete.Id)
            .ToListAsync();

        var categoryIds = eventCategories.Select(ec => ec.CategoryId).ToList();

        _context.EventCategories.RemoveRange(eventCategories);
        _context.Events.Remove(eventToDelete);
        await _context.SaveChangesAsync();

        var unusedCategories = await _context.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .Where(c => !_context.EventCategories.Any(ec => ec.CategoryId == c.Id))
            .ToListAsync();

        _context.Categories.RemoveRange(unusedCategories);
        await _context.SaveChangesAsync();
    }

    public async Task AddEventCategoryConnAsync(Guid newEventId, Guid categoryId)
    {
        var eventCategory = new EventCategory
        {
            EventId = newEventId,
            CategoryId = categoryId
        };

        await _context.EventCategories.AddAsync(eventCategory);
        await _context.SaveChangesAsync();
    }
}