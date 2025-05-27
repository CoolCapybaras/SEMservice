using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface INotificationService
{
    Task AddNotificationAsync(Notification notification);
    Task<List<Notification>> GetNotificationsForUserAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
}