using System.Security.Claims;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Models;
using SEM.Services;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/events/{eventId}/board/columns")]
public class BoardColumnsController: ControllerBase
{
    private readonly IBoardColumnService _service;

    public BoardColumnsController(IBoardColumnService service)
    {
        _service = service;
    }
    
    /// <summary>
    /// Создать колонку канбан доски
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateColumn(Guid eventId, [FromBody] BoardColumnRequest column)
    {
        var userId = GetUserIdFromToken();
        var result = await _service.CreateColumnAsync(eventId, column.Name, userId);
        if (!result.Success)
            return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    /// <summary>
    /// Получить колонки канбан доски мероприятия
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetColumns(Guid eventId)
    {
        var result = await _service.GetColumnsAsync(eventId);
        if (!result.Success)
            return NotFound(new { error = result.Error });
        return Ok(result.Data);
    }

    /// <summary>
    /// Получить колонку канбан доски по ID
    /// </summary>
    [HttpGet("{columnId}")]
    [Authorize]
    public async Task<IActionResult> GetColumnById(Guid columnId)
    {
        var result = await _service.GetColumnByIdAsync(columnId);
        if (!result.Success || result.Data == null)
            return NotFound(new { error = result.Error });
        return Ok(result.Data);
    }

    /// <summary>
    /// Обновить колонку
    /// </summary>
    [HttpPut("{columnId}")]
    [Authorize]
    public async Task<IActionResult> UpdateColumn(Guid columnId, [FromBody] BoardColumnUpdateRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _service.UpdateColumnAsync(columnId, request, userId);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Удалить колонку
    /// </summary>
    [HttpDelete("{columnId}")]
    [Authorize]
    public async Task<IActionResult> DeleteColumn(Guid columnId)
    {
        var userId = GetUserIdFromToken();
        var result = await _service.DeleteColumnAsync(columnId, userId);
        if (!result.Success)
            return BadRequest(new { error = result.Error });
        return NoContent();
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