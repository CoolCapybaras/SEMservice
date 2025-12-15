using Domain;
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

    public async Task<ServiceResult<bool>> AddNotificationAsync(Notification notification)
    {
        await _notificationRepository.AddNotificationAsync(notification);
        return ServiceResult<bool>.Ok(true);
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
}