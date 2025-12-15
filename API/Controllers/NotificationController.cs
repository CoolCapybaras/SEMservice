using System.Security.Claims;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Получить уведомления текущего пользователя
    /// </summary>
    [HttpGet()]
    [Authorize]
    public async Task<IActionResult> GetNotifications(int count, int offset)
    {
        var userId = GetUserIdFromToken();
        var result = await _notificationService.GetNotificationsForUserAsync(userId, count, offset);
        return Ok(new
        {
            unreadCount = result.AdditionalData?["unreadCount"],
            notifications = result.Data
        });
    }
    
    /// <summary>
    /// Получить уведомление по ID
    /// </summary>
    [HttpGet("{notificationId}")]
    [Authorize]
    public async Task<IActionResult> GetNotificationById(Guid notificationId)
    {
        var result = await _notificationService.GetByIdAsync(notificationId);
        return Ok(new { result = result.Data });
    }

    /// <summary>
    /// Прочитать список уведомлений
    /// </summary>
    [HttpPost("read")]
    [Authorize]
    public async Task<IActionResult> MarkAsRead(List<Guid> notificationId)
    {
        await _notificationService.MarkAsReadAsync(notificationId);
        return Ok();
    }

    /// <summary>
    /// Прочитать все уведомления
    /// </summary>
    [HttpPost("read-all")]
    [Authorize]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserIdFromToken();
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok();
    }
    
    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new Exception("Некорректный идентификатор пользователя в токене");
        }
        
        return userId;
    }
}