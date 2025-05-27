using SEM.Domain.Models;

namespace SEM.Domain.Interfaces;

public interface IEventPostRepository
{
    Task AddPostAsync(EventPost post);
    Task<EventPost?> GetPostByIdAsync(Guid postId);
    Task<List<EventPost>> GetPostsByEventIdAsync(Guid eventId);
    Task DeletePostAsync(Guid postId);
    Task UpdatePostAsync(EventPost post);
}