using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class EventAttachmentRepository : IEventAttachmentRepository
{
    private readonly ApplicationDbContext _context;

    public EventAttachmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EventAttachment> AddAsync(EventAttachment attachment)
    {
        _context.EventAttachments.Add(attachment);
        await _context.SaveChangesAsync();
        return attachment;
    }

    public async Task<List<EventAttachment>> GetByEventIdAsync(Guid eventId)
    {
        return await _context.EventAttachments
            .Include(a => a.Author)
            .Where(a => a.EventId == eventId)
            .ToListAsync();
    }

    public async Task<EventAttachment?> GetByIdAsync(Guid attachmentId)
    {
        return await _context.EventAttachments
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Id == attachmentId);
    }

    public async Task DeleteAsync(EventAttachment attachment)
    {
        _context.EventAttachments.Remove(attachment);
        await _context.SaveChangesAsync();
    }
}