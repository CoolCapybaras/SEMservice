using System.Text.Json;
using Domain;
using Domain.Interfaces;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.Services;

public class EventPostService : IEventPostService
{
    private readonly IEventPostRepository _eventPostRepository;
    private readonly INotificationService _notificationService;
    private readonly IEventService _eventService;

    public EventPostService(IEventPostRepository eventPostRepository, IEventService eventService, INotificationService notificationService)
    {
        _eventPostRepository = eventPostRepository;
        _notificationService = notificationService;
        _eventService = eventService;
        
    }

    public async Task<ServiceResult<EventPost>> AddPostAsync(Guid eventId, Guid authorId, string text)
    {
        var @event = await _eventService.GetEventByIdAsync(eventId);
        if (@event == null)
            return ServiceResult<EventPost>.Fail("Мероприятие не найдено");

        if (@event.Data.ResponsiblePersonId != authorId)
            return ServiceResult<EventPost>.Fail("Вы не являетесь владельцем мероприятия");
        
        if (@event.Data.status == "FINISHED")
            return ServiceResult<EventPost>.Fail("Мероприятие завершено");

        var post = new EventPost
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            AuthorId = authorId,
            Text = text,
            CreatedAt = DateTime.UtcNow
        };

        await _eventPostRepository.AddPostAsync(post);

        // Рассылка уведомлений
        var payload = new
        {
            post_id = post.Id,
            community_id = post.EventId,
            community_name = @event.Data.Name,
            text = post.Text,
            created_at = post.CreatedAt
        };

        var subscribers = await _eventService.GetAllSuscribersWithoutOffsetAsync(post.EventId, null);
        foreach (var user in subscribers.Data)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = user.id,
                Type = "Post",
                Payload = JsonSerializer.Serialize(payload),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationService.AddNotificationAsync(notification);
        }

        return ServiceResult<EventPost>.Ok(post);
    }

    public async Task<ServiceResult<EventPost?>> GetPostByIdAsync(Guid eventId, Guid postId)
    {
        var curEvent = await _eventService.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<EventPost>.Fail("Мероприятие не найдено");
        var post = await _eventPostRepository.GetPostByIdAsync(eventId, postId);
        if (post == null)
            return ServiceResult<EventPost?>.Fail("Пост не найден");

        return ServiceResult<EventPost?>.Ok(post);
    }

    public async Task<ServiceResult<List<EventPost>>> GetPostsByEventIdAsync(Guid eventId, int count, int offset)
    {
        var posts = await _eventPostRepository.GetPostsByEventIdAsync(eventId, count, offset);
        if (posts.Count == 0)
            return ServiceResult<List<EventPost>>.Fail("Постов нет или ивент не найден");
        return ServiceResult<List<EventPost>>.Ok(posts);
    }
    
    public async Task<ServiceResult<bool>> DeletePostAsync(Guid postId, Guid eventId, Guid authorId)
    {
        var @event = await _eventService.GetEventByIdAsync(eventId);
        if (@event == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");

        if (@event.Data.ResponsiblePersonId != authorId)
            return ServiceResult<bool>.Fail("Вы не являетесь владельцем мероприятия");

        var post = await _eventPostRepository.GetPostByIdAsync(eventId, postId);
        if (post == null)
            return ServiceResult<bool>.Fail("Пост не найден");

        await _eventPostRepository.DeletePostAsync(postId);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<EventPost>> UpdatePostAsync(Guid postId, Guid eventId, Guid authorId, string text)
    {
        var post = await _eventPostRepository.GetPostByIdAsync(eventId, postId);
        if (post == null)
            return ServiceResult<EventPost>.Fail("Пост не найден");

        var @event = await _eventService.GetEventByIdAsync(eventId);
        if (@event == null)
            return ServiceResult<EventPost>.Fail("Мероприятие не найдено");

        if (@event.Data.ResponsiblePersonId != authorId)
            return ServiceResult<EventPost>.Fail("Вы не являетесь владельцем мероприятия");
        
        if (@event.Data.status == "FINISHED")
            return ServiceResult<EventPost>.Fail("Мероприятие завершено");

        var textChanged = post.Text != text;
        post.Text = text;
        await _eventPostRepository.UpdatePostAsync(post);

        if (textChanged)
            await SendPostNotificationAsync(post, @event.Data, "PostUpdated");

        return ServiceResult<EventPost>.Ok(post);
    }
    
    private async Task SendPostNotificationAsync(EventPost post, Event @event, string type)
    {
        var payload = new
        {
            post_id = post.Id,
            community_id = post.EventId,
            community_name = @event.Name,
            text = post.Text,
            created_at = post.CreatedAt
        };

        var subscribers = await _eventService.GetAllSuscribersWithoutOffsetAsync(post.EventId, null);
        foreach (var user in subscribers.Data)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = user.id,
                Type = type,
                Payload = JsonSerializer.Serialize(payload),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationService.AddNotificationAsync(notification);
        }
    }
}