using SEM.Domain.Models;

namespace SEM.Infrastructure.Repositories;

public interface INotificationRepository
{
    Task AddNotificationAsync(Notification notification);
    Task<Notification> GetByIdAsync(Guid id);
    Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int count, int offset);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(List<Guid> notificationIds);
    Task MarkAllAsReadAsync(Guid userId);
}