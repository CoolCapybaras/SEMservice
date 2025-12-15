using System.Security.Claims;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Models;

namespace SEM.API.Controllers;

[Route("api/events/{eventId}/board")]
[ApiController]
public class BoardTasksController : ControllerBase
{
    private readonly IBoardTaskService _service;

    public BoardTasksController(IBoardTaskService service)
    {
        _service = service;
    }

    /// <summary>
    /// Создать задачу
    /// </summary>
    [HttpPost("columns/{columnId}/tasks")]
    [Authorize]
    public async Task<IActionResult> CreateTask(Guid columnId, [FromBody] BoardTaskCreateRequest task)
    {
        var userId = GetUserIdFromToken();
        var result = await _service.CreateTaskAsync(columnId, task.Title, task.Description, task.AssignedUserId,
            task.DueDate, userId);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Получить задачи из колонки
    /// </summary>
    [HttpGet("columns/{columnId}/tasks")]
    [Authorize]
    public async Task<IActionResult> GetTasks(Guid columnId)
    {
        var result = await _service.GetTasksAsync(columnId);
        return Ok(result.Data);
    }

    /// <summary>
    /// Получить задачу по ID
    /// </summary>
    [HttpGet("tasks/{taskId}")]
    [Authorize]
    public async Task<IActionResult> GetTaskById(Guid taskId)
    {
        var result = await _service.GetTaskByIdAsync(taskId);
        if (result.Data == null)
            return NotFound();

        return Ok(result.Data);
    }

    /// <summary>
    /// Обновить задачу
    /// </summary>
    [HttpPut("tasks/{taskId}")]
    [Authorize]
    public async Task<IActionResult> UpdateTask(Guid taskId, [FromBody] BoardTaskUpdateRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _service.UpdateTaskAsync(taskId, request, userId);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Удалить задачу
    /// </summary>
    [HttpDelete("tasks/{taskId}")]
    [Authorize]
    public async Task<IActionResult> DeleteTask(Guid taskId)
    {
        var userId = GetUserIdFromToken();
        var result = await _service.DeleteTaskAsync(taskId, userId);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new Exception("Некорректный идентификатор пользователя в токене");

        return userId;
    }
}