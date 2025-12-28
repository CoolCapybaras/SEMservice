using System.Security.Claims;
using System.Threading.Tasks;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly IEventService _eventService;

    public CategoryController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Получить все возможные категории
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllCategories()
    {
        var result = await _eventService.GetAllCategoriesAsync();
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Добавить категорию в мероприятие
    /// </summary>
    [HttpPost("{eventId}")]
    [Authorize]
    public async Task<IActionResult> AddCategoryToEvent(Guid eventId, [FromBody] AddCategoryRequest request)
    {
        var userId = GetUserIdFromToken();
        
        var result = await _eventService.AddCategoryToEventAsync(eventId, request.Name, userId);
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