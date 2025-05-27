using System.Text.Json;
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

    public async Task AddPostAsync(EventPost post)
    {
        await _eventPostRepository.AddPostAsync(post);
        var @event = await _eventService.GetEventByIdAsync(post.EventId);
        
        var payload = new
        {
            post_id = post.Id,
            community_id = post.EventId,
            community_name = @event.Name,
            text = post.Text,
            created_at = post.CreatedAt
        };
        
        var subscribers = await _eventService.GetAllSuscribersAsync(post.EventId);

        foreach (var user in subscribers)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = "Post",
                Payload = JsonSerializer.Serialize(payload),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationService.AddNotificationAsync(notification);
        }
        

    }

    public Task<EventPost?> GetPostByIdAsync(Guid postId)
    {
        return _eventPostRepository.GetPostByIdAsync(postId);
    }

    public Task<List<EventPost>> GetPostsByEventIdAsync(Guid eventId)
    {
        return _eventPostRepository.GetPostsByEventIdAsync(eventId);
    }
    
    public Task DeletePostAsync(Guid postId)
    {
        return _eventPostRepository.DeletePostAsync(postId);
    }

    public Task UpdatePostAsync(EventPost post)
    {
        return _eventPostRepository.UpdatePostAsync(post);
    }
}