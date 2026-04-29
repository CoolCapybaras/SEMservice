using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IEventNoteRepository
{
    Task<EventNote> AddAsync(EventNote note);
    Task<EventNote?> GetByIdAsync(Guid noteId);
    Task<List<EventNote>> GetByEventIdAsync(Guid eventId);
    Task<EventNote> UpdateAsync(EventNote note);
}