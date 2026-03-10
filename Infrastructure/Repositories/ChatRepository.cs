using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly ApplicationDbContext _context;

    public ChatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<EventChatMessage>> GetMessagesAsync(Guid eventId, int count, int offset)
    {
        return await _context.EventChatMessages
            .Where(m => m.EventId == eventId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(count)
            .ToListAsync();
    }

    public async Task AddMessageAsync(EventChatMessage message)
    {
        await _context.EventChatMessages.AddAsync(message);
        await _context.SaveChangesAsync();
    }
}