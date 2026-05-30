using Domain.DTO;

namespace Domain.Interfaces;

public interface IEventNoteService
{
    Task<ServiceResult<EventNoteResponse>> CreateAsync(Guid eventId, Guid userId, string text);
    Task<ServiceResult<EventNoteResponse>> UpdateAsync(Guid eventId, Guid noteId, Guid userId, string text);
    Task<ServiceResult<List<EventNoteResponse>>> GetByEventAsync(Guid eventId, Guid userId);
    Task<ServiceResult<bool>> DeleteAsync(Guid eventId, Guid noteId, Guid userId);
}