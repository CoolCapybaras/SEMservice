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
            .Include(m => m.Attachments)
            .Include(m => m.User)
            .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.User)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<EventChatMessage>> SearchMessagesByTextAsync(Guid eventId, string searchText, int maxResults = 500)
    {
        var pattern = "%" + searchText + "%";
        return await _context.EventChatMessages
            .Where(m => m.EventId == eventId && EF.Functions.ILike(m.Text, pattern))
            .Include(m => m.Attachments)
            .Include(m => m.User)
            .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.User)
            .OrderBy(m => m.CreatedAt)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<EventChatMessage?> GetMessageByIdAsync(Guid messageId)
    {
        return await _context.EventChatMessages
            .Include(m => m.Attachments)
            .Include(m => m.User)
            .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.User)
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }

    public async Task AddMessageAsync(EventChatMessage message)
    {
        await _context.EventChatMessages.AddAsync(message);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateMessageAsync(EventChatMessage message)
    {
        _context.EventChatMessages.Update(message);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteMessageAsync(Guid messageId)
    {
        var m = await _context.EventChatMessages
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == messageId);
        if (m == null)
            return;
        _context.EventChatMessages.Remove(m);
        await _context.SaveChangesAsync();
    }

    public async Task AddAttachmentsAsync(IEnumerable<EventChatAttachment> attachments)
    {
        await _context.EventChatAttachments.AddRangeAsync(attachments);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAttachmentAsync(EventChatAttachment attachment)
    {
        _context.EventChatAttachments.Remove(attachment);
        await _context.SaveChangesAsync();
    }

    public async Task<EventChatAttachment?> GetAttachmentByIdAsync(Guid attachmentId)
    {
        return await _context.EventChatAttachments
            .Include(a => a.Message)
            .FirstOrDefaultAsync(a => a.Id == attachmentId);
    }
}