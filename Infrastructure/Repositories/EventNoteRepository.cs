using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class EventNoteRepository : IEventNoteRepository
{
    private readonly ApplicationDbContext _context;

    public EventNoteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EventNote> AddAsync(EventNote note)
    {
        _context.EventNotes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    public async Task<EventNote?> GetByIdAsync(Guid noteId)
    {
        return await _context.EventNotes
            .Include(n => n.Author)
            .FirstOrDefaultAsync(n => n.Id == noteId);
    }

    public async Task<List<EventNote>> GetByEventIdAsync(Guid eventId)
    {
        return await _context.EventNotes
            .Where(n => n.EventId == eventId)
            .Include(n => n.Author)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<EventNote> UpdateAsync(EventNote note)
    {
        _context.EventNotes.Update(note);
        await _context.SaveChangesAsync();
        return note;
    }
    
    public async Task DeleteAsync(EventNote note)
    {
        _context.EventNotes.Remove(note);
        await _context.SaveChangesAsync();
    }
}