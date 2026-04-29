using Domain;
using Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;
using SEM.Infrastructure.Repositories;
using SEM.Services.Hubs;

namespace SEM.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IHubContext<NotificationHub> _notificationHubContext;

    public NotificationService(
        INotificationRepository notificationRepository,
        IUserProfileRepository userProfileRepository,
        IHubContext<NotificationHub> notificationHubContext)
    {
        _notificationRepository = notificationRepository;
        _userProfileRepository = userProfileRepository;
        _notificationHubContext = notificationHubContext;
    }

    public async Task<ServiceResult<bool>> AddNotificationAsync(Notification notification)
    {
        await _notificationRepository.AddNotificationAsync(notification);
        await _notificationHubContext.Clients.Group(notification.UserId.ToString())
            .SendAsync("NotificationReceived", notification);
        return ServiceResult<bool>.Ok(true);
    }
    
    public async Task<ServiceResult<bool>> AddNotificationIfEnabledAsync(Notification notification)
    {
        var user = await _userProfileRepository.GetByIdAsync(notification.UserId);
        if (user == null)
            return ServiceResult<bool>.Fail("Пользователь не найден");

        if (!IsEnabledForType(user, notification.Type))
            return ServiceResult<bool>.Ok(false);

        // In-app уведомления (БД + SignalR) всегда создаются для включенных типов.
        // NotificationChannel здесь не блокирует внутреннюю доставку, а нужен для внешних каналов.
        return await AddNotificationAsync(notification);
    }

    public async Task<ServiceResult<Notification>> GetByIdAsync(Guid notificationId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        return ServiceResult<Notification>.Ok(notification);
    }

    public async Task<ServiceResult<List<Notification>>> GetNotificationsForUserAsync(Guid userId, int count, int offset)
    {
        var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, count, offset);
        var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);

        var result = ServiceResult<List<Notification>>.Ok(notifications);
        result.AdditionalData = new Dictionary<string, object>
        {
            { "unreadCount", unreadCount }
        };

        return result;
    }

    public async Task<ServiceResult<bool>> MarkAsReadAsync(List<Guid> notificationIds)
    {
        await _notificationRepository.MarkAsReadAsync(notificationIds);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> MarkAllAsReadAsync(Guid userId)
    {
        await _notificationRepository.MarkAllAsReadAsync(userId);
        return ServiceResult<bool>.Ok(true);
    }

    private static bool IsEnabledForType(User user, string type)
    {
        return type switch
        {
            "TaskAssigned" => user.NotifyTaskAssigned,
            "TaskDeadline" => user.NotifyTaskDeadline,
            "EventStart" => user.NotifyEventStart,
            "EventCancelled" => user.NotifyEventCancelled,
            _ => true
        };
    }
}