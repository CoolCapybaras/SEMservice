using System.Text.Json;
using Domain;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;
using SEM.Services.Hubs;

namespace SEM.Services;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<ChatHub> _chatHubContext;

    public ChatService(
        IChatRepository chatRepository,
        IEventRepository eventRepository,
        IUserProfileRepository userProfileRepository,
        INotificationService notificationService,
        IHubContext<ChatHub> chatHubContext)
    {
        _chatRepository = chatRepository;
        _eventRepository = eventRepository;
        _userProfileRepository = userProfileRepository;
        _notificationService = notificationService;
        _chatHubContext = chatHubContext;
    }

    public async Task<ServiceResult<List<ChatMessageResponseDto>>> GetMessagesAsync(Guid eventId, int count, int offset, Guid userId)
    {
        var @event = await _eventRepository.GetEventByIdAsync(eventId);
        if (@event == null)
            return ServiceResult<List<ChatMessageResponseDto>>.Fail("Мероприятие не найдено");

        var isParticipant = await IsUserParticipantAsync(eventId, userId, @event.ResponsiblePersonId);
        if (!isParticipant)
            return ServiceResult<List<ChatMessageResponseDto>>.Fail("Вы не являетесь участником мероприятия");

        var messages = await _chatRepository.GetMessagesAsync(eventId, count, offset);
        var dto = messages.Select(MapToResponse).ToList();
        return ServiceResult<List<ChatMessageResponseDto>>.Ok(dto);
    }
    
    public async Task<ServiceResult<List<ChatMessageResponseDto>>> SearchMessagesAsync(
        Guid eventId,
        string text,
        Guid userId,
        int maxResults = 500)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ServiceResult<List<ChatMessageResponseDto>>.Fail("Введите текст для поиска");

        var @event = await _eventRepository.GetEventByIdAsync(eventId);
        if (@event == null)
            return ServiceResult<List<ChatMessageResponseDto>>.Fail("Мероприятие не найдено");

        var isParticipant = await IsUserParticipantAsync(eventId, userId, @event.ResponsiblePersonId);
        if (!isParticipant)
            return ServiceResult<List<ChatMessageResponseDto>>.Fail("Вы не являетесь участником мероприятия");

        var trimmed = text.Trim();
        var capped = Math.Clamp(maxResults, 1, 1000);
        var messages = await _chatRepository.SearchMessagesByTextAsync(eventId, trimmed, capped);
        var dto = messages.Select(MapToResponse).ToList();
        return ServiceResult<List<ChatMessageResponseDto>>.Ok(dto);
    }


    public async Task<ServiceResult<EventChatMessage>> AddMessageAsync(
        Guid eventId,
        Guid userId,
        string text,
        List<EventChatAttachment>? attachments = null,
        Guid? replyToMessageId = null)
    {
        var hasAttachments = attachments is { Count: > 0 };
        if (string.IsNullOrWhiteSpace(text) && !hasAttachments)
            return ServiceResult<EventChatMessage>.Fail("Сообщение должно содержать текст или файлы");

        var @event = await _eventRepository.GetEventByIdAsync(eventId);
        if (@event == null)
            return ServiceResult<EventChatMessage>.Fail("Мероприятие не найдено");

        if (@event.status == "FINISHED")
            return ServiceResult<EventChatMessage>.Fail("Мероприятие завершено, чат недоступен для отправки сообщений");

        var isParticipant = await IsUserParticipantAsync(eventId, userId, @event.ResponsiblePersonId);
        if (!isParticipant)
            return ServiceResult<EventChatMessage>.Fail("Вы не являетесь участником мероприятия");

        if (replyToMessageId.HasValue)
        {
            var parent = await _chatRepository.GetMessageByIdAsync(replyToMessageId.Value);
            if (parent == null || parent.EventId != eventId)
                return ServiceResult<EventChatMessage>.Fail("Сообщение для ответа не найдено");
        }

        var message = new EventChatMessage
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Text = (text ?? string.Empty).Trim(),
            Attachments = attachments ?? new List<EventChatAttachment>(),
            ReplyToMessageId = replyToMessageId
        };

        foreach (var a in message.Attachments)
        {
            if (a.Id == Guid.Empty)
                a.Id = Guid.NewGuid();
            a.MessageId = message.Id;
            a.CreatedAt = DateTime.UtcNow;
        }

        await _chatRepository.AddMessageAsync(message);

        var reloaded = await _chatRepository.GetMessageByIdAsync(message.Id);
        if (reloaded == null)
            return ServiceResult<EventChatMessage>.Fail("Не удалось сохранить сообщение");

        var sender = await _userProfileRepository.GetByIdAsync(userId);
        var messageDto = MapToResponse(reloaded, sender);

        await _chatHubContext.Clients.Group(eventId.ToString())
            .SendAsync("MessageReceived", messageDto);

        var recipientIds = new HashSet<Guid>();

        if (@event.ResponsiblePersonId != Guid.Empty)
            recipientIds.Add(@event.ResponsiblePersonId);

        var subscribers = await _eventRepository.GetAllSuscribersWithoutOffsetAsync(eventId, null, null);
        foreach (var u in subscribers)
            recipientIds.Add(u.id);

        recipientIds.Remove(userId);

        foreach (var recipientId in recipientIds)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = recipientId,
                Type = "ChatMessage",
                Payload = JsonSerializer.Serialize(new
                {
                    community_id = @event.Id,
                    community_name = @event.Name,
                    message = messageDto
                }),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationService.AddNotificationAsync(notification);
        }

        return ServiceResult<EventChatMessage>.Ok(reloaded);
    }

    public async Task<ServiceResult<ChatMessageResponseDto>> UpdateMessageAsync(
        Guid eventId,
        Guid messageId,
        Guid userId,
        string? text,
        List<Guid>? removeAttachmentIds)
    {
        var message = await _chatRepository.GetMessageByIdAsync(messageId);
        if (message == null || message.EventId != eventId)
            return ServiceResult<ChatMessageResponseDto>.Fail("Сообщение не найдено");

        if (message.UserId != userId)
            return ServiceResult<ChatMessageResponseDto>.Fail("Можно редактировать только свои сообщения");

        var @event = await _eventRepository.GetEventByIdAsync(eventId);
        if (@event == null)
            return ServiceResult<ChatMessageResponseDto>.Fail("Мероприятие не найдено");

        if (@event.status == "FINISHED")
            return ServiceResult<ChatMessageResponseDto>.Fail("Мероприятие завершено, чат недоступен");

        if (removeAttachmentIds is { Count: > 0 })
        {
            foreach (var attId in removeAttachmentIds.Distinct())
            {
                var att = await _chatRepository.GetAttachmentByIdAsync(attId);
                if (att == null || att.MessageId != messageId)
                    continue;
                TryDeletePhysicalFile(att.FilePath);
                await _chatRepository.RemoveAttachmentAsync(att);
            }
        }

        message = await _chatRepository.GetMessageByIdAsync(messageId);
        if (message == null)
            return ServiceResult<ChatMessageResponseDto>.Fail("Сообщение не найдено");

        if (text != null)
            message.Text = text.Trim();

        message.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(message.Text) && message.Attachments.Count == 0)
            return ServiceResult<ChatMessageResponseDto>.Fail("Сообщение должно содержать текст или файлы");

        await _chatRepository.UpdateMessageAsync(message);

        var reloaded = await _chatRepository.GetMessageByIdAsync(messageId);
        if (reloaded == null)
            return ServiceResult<ChatMessageResponseDto>.Fail("Сообщение не найдено");

        var sender = await _userProfileRepository.GetByIdAsync(reloaded.UserId);
        var dto = MapToResponse(reloaded, sender);

        await _chatHubContext.Clients.Group(eventId.ToString())
            .SendAsync("MessageUpdated", dto);

        return ServiceResult<ChatMessageResponseDto>.Ok(dto);
    }

    public async Task<ServiceResult<ChatMessageResponseDto>> AddAttachmentsToMessageAsync(
        Guid eventId,
        Guid messageId,
        Guid userId,
        List<EventChatAttachment> newAttachments)
    {
        if (newAttachments is not { Count: > 0 })
            return ServiceResult<ChatMessageResponseDto>.Fail("Добавьте файлы");

        var message = await _chatRepository.GetMessageByIdAsync(messageId);
        if (message == null || message.EventId != eventId)
            return ServiceResult<ChatMessageResponseDto>.Fail("Сообщение не найдено");

        if (message.UserId != userId)
            return ServiceResult<ChatMessageResponseDto>.Fail("Можно редактировать только свои сообщения");

        var @event = await _eventRepository.GetEventByIdAsync(eventId);
        if (@event == null)
            return ServiceResult<ChatMessageResponseDto>.Fail("Мероприятие не найдено");

        if (@event.status == "FINISHED")
            return ServiceResult<ChatMessageResponseDto>.Fail("Мероприятие завершено, чат недоступен");

        foreach (var a in newAttachments)
        {
            if (a.Id == Guid.Empty)
                a.Id = Guid.NewGuid();
            a.MessageId = messageId;
            a.CreatedAt = DateTime.UtcNow;
        }

        await _chatRepository.AddAttachmentsAsync(newAttachments);

        var reloaded = await _chatRepository.GetMessageByIdAsync(messageId);
        if (reloaded == null)
            return ServiceResult<ChatMessageResponseDto>.Fail("Сообщение не найдено");

        reloaded.UpdatedAt = DateTime.UtcNow;
        await _chatRepository.UpdateMessageAsync(reloaded);

        reloaded = await _chatRepository.GetMessageByIdAsync(messageId);
        if (reloaded == null)
            return ServiceResult<ChatMessageResponseDto>.Fail("Сообщение не найдено");

        var sender = await _userProfileRepository.GetByIdAsync(reloaded.UserId);
        var dto = MapToResponse(reloaded, sender);

        await _chatHubContext.Clients.Group(eventId.ToString())
            .SendAsync("MessageUpdated", dto);

        return ServiceResult<ChatMessageResponseDto>.Ok(dto);
    }

    public async Task<ServiceResult<ChatMessageResponseDto>> RemoveAttachmentFromMessageAsync(
        Guid eventId,
        Guid messageId,
        Guid userId,
        Guid attachmentId)
    {
        return await UpdateMessageAsync(eventId, messageId, userId, text: null, removeAttachmentIds: new List<Guid> { attachmentId });
    }

    public async Task<ServiceResult<bool>> DeleteMessageAsync(Guid eventId, Guid messageId, Guid userId)
    {
        var message = await _chatRepository.GetMessageByIdAsync(messageId);
        if (message == null || message.EventId != eventId)
            return ServiceResult<bool>.Fail("Сообщение не найдено");

        if (message.UserId != userId)
            return ServiceResult<bool>.Fail("Можно удалять только свои сообщения");

        var @event = await _eventRepository.GetEventByIdAsync(eventId);
        if (@event == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");

        if (@event.status == "FINISHED")
            return ServiceResult<bool>.Fail("Мероприятие завершено, чат недоступен");

        foreach (var att in message.Attachments)
            TryDeletePhysicalFile(att.FilePath);

        await _chatRepository.DeleteMessageAsync(messageId);

        await _chatHubContext.Clients.Group(eventId.ToString())
            .SendAsync("MessageDeleted", new { messageId });

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<EventChatAttachment>> GetAttachmentForDownloadAsync(Guid eventId, Guid userId, Guid attachmentId)
    {
        var @event = await _eventRepository.GetEventByIdAsync(eventId);
        if (@event == null)
            return ServiceResult<EventChatAttachment>.Fail("Мероприятие не найдено");

        var isParticipant = await IsUserParticipantAsync(eventId, userId, @event.ResponsiblePersonId);
        if (!isParticipant)
            return ServiceResult<EventChatAttachment>.Fail("Вы не являетесь участником мероприятия");

        var attachment = await _chatRepository.GetAttachmentByIdAsync(attachmentId);
        if (attachment == null || attachment.Message == null)
            return ServiceResult<EventChatAttachment>.Fail("Файл не найден");

        if (attachment.Message.EventId != eventId)
            return ServiceResult<EventChatAttachment>.Fail("Файл не найден");

        return ServiceResult<EventChatAttachment>.Ok(attachment);
    }

    private static void TryDeletePhysicalFile(string relativePath)
    {
        var full = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
            relativePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (File.Exists(full))
        {
            try
            {
                File.Delete(full);
            }
            catch
            {
                /* ignore */
            }
        }
    }

    private static ChatMessageResponseDto MapToResponse(EventChatMessage m) =>
        MapToResponse(m, m.User);

    private static ChatMessageResponseDto MapToResponse(EventChatMessage m, User? sender)
    {
        return new ChatMessageResponseDto
        {
            Id = m.Id,
            Text = m.Text,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt,
            ReplyToMessageId = m.ReplyToMessageId,
            ReplyTo = MapReplyPreview(m.ReplyToMessage),
            Sender = new ChatSenderDto
            {
                Id = m.UserId,
                FirstName = sender?.FirstName ?? m.User?.FirstName ?? "User",
                LastName = sender?.LastName ?? m.User?.LastName,
                AvatarUrl = sender?.AvatarUrl ?? m.User?.AvatarUrl
            },
            Attachments = m.Attachments.Select(a => new ChatAttachmentDto
            {
                Id = a.Id,
                FilePath = a.FilePath,
                OriginalFileName = a.OriginalFileName,
                ContentType = a.ContentType,
                Size = a.Size
            }).ToList()
        };
    }

    private static ChatReplyPreviewDto? MapReplyPreview(EventChatMessage? reply)
    {
        if (reply == null)
            return null;

        var previewText = reply.Text;
        const int maxLen = 500;
        if (previewText.Length > maxLen)
            previewText = previewText[..maxLen] + "…";

        return new ChatReplyPreviewDto
        {
            Id = reply.Id,
            Text = previewText,
            Sender = new ChatSenderDto
            {
                Id = reply.UserId,
                FirstName = reply.User?.FirstName ?? "User",
                LastName = reply.User?.LastName,
                AvatarUrl = reply.User?.AvatarUrl
            }
        };
    }

    private async Task<bool> IsUserParticipantAsync(Guid eventId, Guid userId, Guid responsiblePersonId)
    {
        if (userId == responsiblePersonId)
            return true;

        var subscribers = await _eventRepository.GetAllSuscribersWithoutOffsetAsync(eventId, null, null);
        return subscribers.Any(u => u.id == userId);
    }
}