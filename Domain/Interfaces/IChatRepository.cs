using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IChatRepository
{
    Task<List<EventChatMessage>> GetMessagesAsync(Guid eventId, int count, int offset);
    Task<List<EventChatMessage>> SearchMessagesByTextAsync(Guid eventId, string searchText, int maxResults = 500);
    Task<EventChatMessage?> GetMessageByIdAsync(Guid messageId);
    Task AddMessageAsync(EventChatMessage message);
    Task UpdateMessageAsync(EventChatMessage message);
    Task DeleteMessageAsync(Guid messageId);
    Task AddAttachmentsAsync(IEnumerable<EventChatAttachment> attachments);
    Task RemoveAttachmentAsync(EventChatAttachment attachment);
    Task<EventChatAttachment?> GetAttachmentByIdAsync(Guid attachmentId);
}
