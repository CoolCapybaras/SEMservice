using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IEventPostService
{
    Task<ServiceResult<EventPost>> AddPostAsync(Guid eventId, Guid authorId, string title, string text);
    Task<ServiceResult<EventPost?>> GetPostByIdAsync(Guid eventId, Guid postId);
    Task<ServiceResult<List<EventPost>>> GetPostsByEventIdAsync(Guid eventId, int count, int offset);
    Task<ServiceResult<bool>> DeletePostAsync(Guid postId, Guid eventId, Guid authorId);
    Task<ServiceResult<EventPost>> UpdatePostAsync(Guid postId, Guid eventId, Guid authorId, string title, string text);
}