using System.Text.Json;
using Domain;
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
    private readonly INotificationService _notificationService;
    private readonly IHubContext<ChatHub> _chatHubContext;

    public ChatService(IChatRepository chatRepository, IEventRepository eventRepository, INotificationService notificationService, IHubContext<ChatHub> chatHubContext)
    {
        _chatRepository = chatRepository;
        _eventRepository = eventRepository;
        _notificationService = notificationService;
        _chatHubContext = chatHubContext;
    }

    public async Task<ServiceResult<List<EventChatMessage>>> GetMessagesAsync(Guid eventId, int count, int offset, Guid userId)
    {
        var @event = await _eventRepository.GetEventByIdAsync(eventId);
        if (@event == null)
            return ServiceResult<List<EventChatMessage>>.Fail("Мероприятие не найдено");

        var isParticipant = await IsUserParticipantAsync(eventId, userId, @event.ResponsiblePersonId);
        if (!isParticipant)
            return ServiceResult<List<EventChatMessage>>.Fail("Вы не являетесь участником мероприятия");

        var messages = await _chatRepository.GetMessagesAsync(eventId, count, offset);
        return ServiceResult<List<EventChatMessage>>.Ok(messages);
    }

    public async Task<ServiceResult<EventChatMessage>> AddMessageAsync(Guid eventId, Guid userId, string text, List<EventChatAttachment>? attachments = null)
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

        var message = new EventChatMessage
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Text = (text ?? string.Empty).Trim(),
            Attachments = attachments ?? new List<EventChatAttachment>()
        };

        foreach (var a in message.Attachments)
        {
            if (a.Id == Guid.Empty)
                a.Id = Guid.NewGuid();
            a.MessageId = message.Id;
            a.CreatedAt = DateTime.UtcNow;
        }

        await _chatRepository.AddMessageAsync(message);

        var payload = new
        {
            message_id = message.Id,
            community_id = @event.Id,
            community_name = @event.Name,
            sender_user_id = userId,
            text = message.Text,
            created_at = message.CreatedAt,
            attachments = message.Attachments.Select(a => new
            {
                id = a.Id,
                file_path = a.FilePath,
                original_file_name = a.OriginalFileName,
                content_type = a.ContentType,
                size = a.Size
            })
        };
        
        await _chatHubContext.Clients.Group(eventId.ToString())
            .SendAsync("MessageReceived", payload);

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
                Payload = JsonSerializer.Serialize(payload),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationService.AddNotificationAsync(notification);
        }

        return ServiceResult<EventChatMessage>.Ok(message);
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

    private async Task<bool> IsUserParticipantAsync(Guid eventId, Guid userId, Guid responsiblePersonId)
    {
        if (userId == responsiblePersonId)
            return true;

        var subscribers = await _eventRepository.GetAllSuscribersWithoutOffsetAsync(eventId, null, null);
        return subscribers.Any(u => u.id == userId);
    }
}