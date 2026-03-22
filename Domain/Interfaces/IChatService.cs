using Domain.DTO;
using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface IChatService
{
    Task<ServiceResult<List<ChatMessageResponseDto>>> GetMessagesAsync(Guid eventId, int count, int offset, Guid userId);
    Task<ServiceResult<List<ChatMessageResponseDto>>> SearchMessagesAsync(Guid eventId, string text, Guid userId, int maxResults = 500);
    Task<ServiceResult<EventChatMessage>> AddMessageAsync(Guid eventId, Guid userId, string text, List<EventChatAttachment>? attachments = null, Guid? replyToMessageId = null);
    Task<ServiceResult<ChatMessageResponseDto>> UpdateMessageAsync(Guid eventId, Guid messageId, Guid userId, string? text, List<Guid>? removeAttachmentIds);
    Task<ServiceResult<ChatMessageResponseDto>> AddAttachmentsToMessageAsync(Guid eventId, Guid messageId, Guid userId, List<EventChatAttachment> newAttachments);
    Task<ServiceResult<ChatMessageResponseDto>> RemoveAttachmentFromMessageAsync(Guid eventId, Guid messageId, Guid userId, Guid attachmentId);
    Task<ServiceResult<bool>> DeleteMessageAsync(Guid eventId, Guid messageId, Guid userId);
    Task<ServiceResult<EventChatAttachment>> GetAttachmentForDownloadAsync(Guid eventId, Guid userId, Guid attachmentId);
}