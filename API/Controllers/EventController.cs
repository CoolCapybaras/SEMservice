using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Создать мероприятие
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateEvent([FromBody] EventRequest request)
    {
        var modId = GetUserIdFromToken();
        var result = await _eventService.CreateEventAsync(request, modId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Получить мероприятие по ID
    /// </summary>
    [HttpGet("{eventId}")]
    [Authorize]
    public async Task<IActionResult> GetEventById(Guid eventId)
    {
        var result = await _eventService.GetEventByIdAsync(eventId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Поиск мероприятий по фильтрам
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> SearchEvents([FromQuery] SearchRequest request)
    {
        var result = await _eventService.SearchEventsAsync(request);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Удалить мероприятие
    /// </summary>
    [HttpDelete("{eventId}")]
    [Authorize]
    public async Task<IActionResult> DeleteEvent(Guid eventId)
    {
        var userId = GetUserIdFromToken();
        var result = await _eventService.DeleteEventAsync(eventId, userId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Подписаться на мероприятие
    /// </summary>
    [HttpPost("{eventId}/subscribe")]
    [Authorize]
    public async Task<IActionResult> AddSuscriberAsync(Guid eventId)
    {
        var userId = GetUserIdFromToken();
        var result = await _eventService.AddSuscriberAsync(eventId, userId);
        return result.Success ? Ok(new { result = eventId, userId }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Отписаться от мероприятия
    /// </summary>
    [HttpPost("{eventId}/unsubscribe")]
    [Authorize]
    public async Task<IActionResult> DeleteSuscriber(Guid eventId)
    {
        var userId = GetUserIdFromToken();
        var result = await _eventService.DeleteSuscriber(eventId, userId);
        return result.Success ? Ok(new { result = eventId, userId }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Дать пользователю роль на мероприятии
    /// </summary>
    [HttpPost("{eventId}/users/{userId}/roles/{roleId}")]
    [Authorize]
    public async Task<IActionResult> AddRoleToUser(Guid eventId, Guid userId,Guid roleId)
    {
        var currentUserId = GetUserIdFromToken();
        var result = await _eventService.AddRoleToUser(eventId, userId, roleId, currentUserId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Получить роли в рамках мероприятия
    /// </summary>
    [HttpGet("{eventId}/roles")]
    [Authorize]
    public async Task<IActionResult> GetRolesByEvent(Guid eventId, int count, int offset)
    {
        var result = await _eventService.GetRolesByEvent(eventId, count, offset);
        return result.Success ? Ok(new { res = result.Data }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Получить пользователей подписанных на мероприятие
    /// </summary>
    [HttpGet("{eventId}/subscribers")]
    [Authorize]
    public async Task<IActionResult> GetAllSuscribersAsync(Guid eventId, [FromQuery] string? name, int count, int offset)
    {
        var result = await _eventService.GetAllSuscribersAsync(eventId, name, count, offset);
        return result.Success ? Ok(new { res = result.Data }) : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Обновить данные о мероприятии
    /// </summary>
    [HttpPut("{eventId}")]
    [Authorize]
    public async Task<IActionResult> UpdateEvent(Guid eventId, [FromBody] EventUpdateRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _eventService.UpdateEventAsync(eventId, request, userId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Получить фотографии в рамках мероприятия
    /// </summary>
    [HttpGet("{eventId}/photos")]
    [Authorize]
    public async Task<IActionResult> GetEventPhotos(Guid eventId, int offset, int count)
    {
        var result = await _eventService.GetEventPhotosAsync(eventId, offset, count);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Загрузить фото на мероприятие
    /// </summary>
    [HttpPost("{eventId}/photos")]
    [Consumes("multipart/form-data")]
    [Authorize]
    public async Task<IActionResult> UploadEventPhoto(Guid eventId, IFormFile file)
    {
        var result = await _eventService.AddEventPhotoAsync(eventId, file);
        return result.Success ? Ok(new { path = result.Data }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Сделать пользователя контактом на мероприятии
    /// </summary>
    [HttpPost("{eventId}/contacts")]
    [Authorize]
    public async Task<IActionResult> AddContact(Guid eventId, Guid userId)
    {
        var currentUserId = GetUserIdFromToken();
        var result = await _eventService.AddContact(eventId, userId, currentUserId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Удалить пользователя из контактов мероприятия
    /// </summary>
    [HttpDelete("{eventId}/contacts/{userId}")]
    [Authorize]
    public async Task<IActionResult> DeleteContact(Guid eventId, Guid userId)
    {
        var currentUserId = GetUserIdFromToken();
        var result = await _eventService.DeleteContact(eventId, userId, currentUserId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Получить контакты мероприятия
    /// </summary>
    [HttpGet("{eventId}/contacts")]
    [Authorize]
    public async Task<IActionResult> GetContacts(Guid eventId)
    {
        var result = await _eventService.GetContacts(eventId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
        
    }
    
    /// <summary>
    /// Завершить мероприятие
    /// </summary>
    [HttpPost("{eventId}/finish")]
    [Authorize]
    public async Task<IActionResult> FinishEvent(Guid eventId)
    {
        var userId = GetUserIdFromToken();
        var result = await _eventService.FinishEventAsync(eventId, userId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Создать роль в рамках мероприятия
    /// </summary>
    [HttpPost("{eventId}/roles")]
    [Authorize]
    public async Task<IActionResult> CreateRole(Guid eventId, string roleName)
    {
        var userId = GetUserIdFromToken();
        var result = await _eventService.CreateRoleAsync(roleName, eventId, userId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Получить роль по ID
    /// </summary>
    [HttpGet("{eventId}/roles/{roleId}")]
    [Authorize]
    public async Task<IActionResult> GetRoleById(Guid eventId, Guid roleId)
    {
        var result = await _eventService.GetRoleByIdAsync(roleId, eventId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Изменить имя роли
    /// </summary>
    [HttpPut("{eventId}/roles/{roleId}")]
    [Authorize]
    public async Task<IActionResult> UpdateRole(string newRoleName, Guid eventId, Guid roleId)
    {
        var userId = GetUserIdFromToken();
        var result = await _eventService.UpdateRoleAsync(newRoleName, eventId, roleId, userId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Удалить роль
    /// </summary>
    [HttpDelete("{eventId}/roles/{roleId}")]
    [Authorize]
    public async Task<IActionResult> DeleteRole(Guid eventId, Guid roleId)
    {
        var userId = GetUserIdFromToken();
        var result = await _eventService.DeleteRoleAsync(eventId, roleId, userId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Получить фото мероприятия по ID
    /// </summary>
    [HttpGet("{eventId}/photos/{photoId}")]
    [Authorize]
    public async Task<IActionResult> GetEventPhotoById(Guid eventId, Guid photoId)
    {
        var result = await _eventService.GetEventPhotoByIdAsync(eventId, photoId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Удалить фото мероприятия
    /// </summary>
    [HttpDelete("{eventId}/photos/{photoId}")]
    [Authorize]
    public async Task<IActionResult> DeleteEventPhoto(Guid eventId, Guid photoId)
    {
        var userId = GetUserIdFromToken();
        var result = await _eventService.DeleteEventPhotoAsync(eventId, photoId, userId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
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