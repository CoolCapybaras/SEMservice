using Domain;
using Domain.Interfaces;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.Services;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly IEventRepository _eventRepository;

    public ChatService(IChatRepository chatRepository, IEventRepository eventRepository)
    {
        _chatRepository = chatRepository;
        _eventRepository = eventRepository;
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

    public async Task<ServiceResult<EventChatMessage>> AddMessageAsync(Guid eventId, Guid userId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ServiceResult<EventChatMessage>.Fail("Текст сообщения не может быть пустым");

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
            Text = text.Trim()
        };

        await _chatRepository.AddMessageAsync(message);
        return ServiceResult<EventChatMessage>.Ok(message);
    }

    private async Task<bool> IsUserParticipantAsync(Guid eventId, Guid userId, Guid responsiblePersonId)
    {
        if (userId == responsiblePersonId)
            return true;

        var subscribers = await _eventRepository.GetAllSuscribersWithoutOffsetAsync(eventId, null, null);
        return subscribers.Any(u => u.id == userId);
    }
}