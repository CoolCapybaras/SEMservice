using Domain.Interfaces;
using SEM.Domain.Models;
using SEM.Infrastructure.Repositories;

namespace SEM.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public Task AddNotificationAsync(Notification notification)
    {
        return _notificationRepository.AddNotificationAsync(notification);
    }

    public Task<List<Notification>> GetNotificationsForUserAsync(Guid userId)
    {
        return _notificationRepository.GetUserNotificationsAsync(userId);
    }

    public Task<int> GetUnreadCountAsync(Guid userId)
    {
        return _notificationRepository.GetUnreadCountAsync(userId);
    }

    public Task MarkAsReadAsync(Guid notificationId)
    {
        return _notificationRepository.MarkAsReadAsync(notificationId);
    }
    
    public Task MarkAllAsReadAsync(Guid userId)
    {
        return _notificationRepository.MarkAllAsReadAsync(userId);
    }
}