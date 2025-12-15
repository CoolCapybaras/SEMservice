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
    public async Task<IActionResult> GetBoard(Guid eventId)
    {
        var result = await _service.GetBoardAsync(eventId);
        return Ok(result.Data);
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