using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IEventPostService
{
    Task AddPostAsync(EventPost post);
    Task<EventPost?> GetPostByIdAsync(Guid postId);
    Task<List<EventPost>> GetPostsByEventIdAsync(Guid eventId);
    Task DeletePostAsync(Guid postId);
    Task UpdatePostAsync(EventPost post);
}