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
}