using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IChatService
{
    Task<ServiceResult<List<EventChatMessage>>> GetMessagesAsync(Guid eventId, int count, int offset, Guid userId);
    Task<ServiceResult<EventChatMessage>> AddMessageAsync(Guid eventId, Guid userId, string text, List<EventChatAttachment>? attachments = null);
    Task<ServiceResult<EventChatAttachment>> GetAttachmentForDownloadAsync(Guid eventId, Guid userId, Guid attachmentId);
}