using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IEventAttachmentRepository
{
    Task<EventAttachment> AddAsync(EventAttachment attachment);
    Task<List<EventAttachment>> GetByEventIdAsync(Guid eventId);
    Task<EventAttachment?> GetByIdAsync(Guid attachmentId);
    Task DeleteAsync(EventAttachment attachment);
}