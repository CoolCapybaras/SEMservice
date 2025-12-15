using SEM.Domain.Models;

namespace Domain.Interfaces;

public interface INotificationService
{
    Task<ServiceResult<bool>> AddNotificationAsync(Notification notification);
    Task<ServiceResult<Notification>> GetByIdAsync(Guid notificationId);
    Task<ServiceResult<List<Notification>>> GetNotificationsForUserAsync(Guid userId, int count, int offset);
    Task<ServiceResult<bool>> MarkAsReadAsync(List<Guid> notificationIds);
    Task<ServiceResult<bool>> MarkAllAsReadAsync(Guid userId);
}