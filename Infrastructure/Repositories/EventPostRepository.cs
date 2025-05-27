using Microsoft.EntityFrameworkCore;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;
using SEM.Infrastructure.Data;

namespace SEM.Infrastructure.Repositories;

public class EventPostRepository: IEventPostRepository
{
    private readonly ApplicationDbContext _context;

    public EventPostRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddPostAsync(EventPost post)
    {
        await _context.EventPosts.AddAsync(post);
        await _context.SaveChangesAsync();
    }

    public async Task<EventPost?> GetPostByIdAsync(Guid postId)
    {
        return await _context.EventPosts.FirstOrDefaultAsync(p => p.Id == postId);
    }

    public async Task<List<EventPost>> GetPostsByEventIdAsync(Guid eventId)
    {
        return await _context.EventPosts
            .Where(p => p.EventId == eventId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task DeletePostAsync(Guid postId)
    {
        var post = await _context.EventPosts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post != null)
        {
            _context.EventPosts.Remove(post);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdatePostAsync(EventPost post)
    {
        _context.EventPosts.Update(post);
        await _context.SaveChangesAsync();
    }
}