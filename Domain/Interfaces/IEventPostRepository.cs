using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IEventPostRepository
{
    Task AddPostAsync(EventPost post);
    Task<EventPost?> GetPostByIdAsync(Guid eventId, Guid postId);
    Task<List<EventPost>> GetPostsByEventIdAsync(Guid eventId, int count, int offset);
    Task DeletePostAsync(Guid postId);
    Task UpdatePostAsync(EventPost post);
}