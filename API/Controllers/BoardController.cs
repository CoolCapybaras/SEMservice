using System.Security.Claims;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SEM.API.Controllers;

[Route("api/events/{eventId}/board")]
[ApiController]
public class BoardController : ControllerBase
{
    private readonly IBoardService _service;

    public BoardController(IBoardService service)
    {
        _service = service;
    }

    /// <summary>
    /// Получить доску мероприятия
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetBoard(Guid eventId, [FromQuery] BoardKanbanQuery? query)
    {
        var userId = GetUserIdFromToken();
        var result = await _service.GetBoardAsync(eventId, userId, query);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Список исполнителей по задачам доски (для фильтра).</summary>
    [HttpGet("facets")]
    [Authorize]
    public async Task<IActionResult> GetBoardFacets(Guid eventId)
    {
        var userId = GetUserIdFromToken();
        var result = await _service.GetBoardFacetsAsync(eventId, userId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Перетащить задачу
    /// </summary>
    [HttpPost("tasks/{taskId}/move")]
    [Authorize]
    public async Task<IActionResult> MoveTask(Guid taskId, [FromBody] MoveTaskRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _service.MoveTaskAsync(taskId, request, userId);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new Exception("Некорректный идентификатор пользователя в токене");

        return userId;
    }
}