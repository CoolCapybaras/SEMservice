using Microsoft.EntityFrameworkCore;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class EventRepository: IEventRepository
{
    private readonly ApplicationDbContext _context;

    public EventRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Event> AddEventAsync(Event neEvent)
    {
        await _context.Events.AddAsync(neEvent);
        await _context.SaveChangesAsync();

        return neEvent;
    }

    public async Task<IEnumerable<Event>> GetAllEventsAsync()
    {
        return await _context.Events.ToListAsync();
    }

    public async Task<Event> GetEventByIdAsync(Guid eventId)
    {
        return (await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId))!;
    }

    public async Task<List<Event>> SearchEvents(DateTime? start, DateTime? end, string name, List<string> categories, List<string> organizators, string format,
        bool? isFreePlaces, int offset, int count)
    {
        var query = _context.Events.AsQueryable();
        
        // Фильтр по имени
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(p => EF.Functions.ILike(p.Name, $"{name}%"));
        }
        
        // Фильтр по категориям
        if (categories != null && categories.Any())
        {
            query = query.Where(e => e.EventCategories
                .Any(ec => categories.Contains(ec.Category.Name)));
        }
        
        //Фильтр по временному промежутку
        if(start != null || end != null)
        query = query.Where(e => e.StartDate <= end && e.EndDate >= start);
        
        //Фильтр по организаторам
        if (organizators != null && organizators.Any())
        {
            query = query.Where(e => organizators.Contains(e.ResponsiblePerson));
        }
        
        //Фильтр по формату
        if (!string.IsNullOrWhiteSpace(format))
        {
            query = query.Where(e => e.Format == format);
        }
        
        //Фильтр ао свободным местам
            if (isFreePlaces==true)
            {
                query = query.Where(e => e.MaxParticipants == null);
            }

        query = query.OrderBy(e => e.StartDate)
            .Skip(offset)
            .Take(count);
        
        return await query.ToListAsync();
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
        

        // Сохраняем id связанных категорий
        var relatedCategoryIds = eventToDelete.EventCategories
            .Select(ec => ec.CategoryId)
            .ToList();
        
        _context.EventCategories.RemoveRange(eventToDelete.EventCategories);
        // Удаляем сам ивент
        _context.Events.Remove(eventToDelete);

        // Ищем категории, которые больше не связаны ни с одним ивентом
        var unusedCategories = await _context.Categories
            .Where(c => relatedCategoryIds.Contains(c.id))
            .Where(c => !_context.EventCategories.Any(ec => ec.CategoryId == c.id))
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