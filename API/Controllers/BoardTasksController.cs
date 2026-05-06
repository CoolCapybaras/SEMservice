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
            task.DueDate, userId, task.Priority);

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
    
    /// <summary>
    /// Получить мои назначенные задачи в рамках мероприятия
    /// </summary>
    [HttpGet("event/my-tasks")]
    [Authorize]
    public async Task<IActionResult> GetMyTasksByEvent(Guid eventId)
    {
        var userId = GetUserIdFromToken();
        var result = await _service.GetCurrentUserTasksByEventAsync(eventId, userId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Добавить комментарий к задаче
    /// </summary>
    [HttpPost("tasks/{taskId}/comments")]
    [Authorize]
    public async Task<IActionResult> AddComment(Guid taskId, [FromBody] BoardTaskCommentRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _service.AddCommentAsync(taskId, userId, request.Text);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Получить комментарии задачи
    /// </summary>
    [HttpGet("tasks/{taskId}/comments")]
    [Authorize]
    public async Task<IActionResult> GetComments(Guid taskId)
    {
        var userId = GetUserIdFromToken();
        var result = await _service.GetCommentsAsync(taskId, userId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Получить историю изменений задачи
    /// </summary>
    [HttpGet("tasks/{taskId}/history")]
    [Authorize]
    public async Task<IActionResult> GetTaskHistory(Guid taskId)
    {
        var userId = GetUserIdFromToken();
        var result = await _service.GetHistoryAsync(taskId, userId);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new Exception("Некорректный идентификатор пользователя в токене");

        return userId;
    }
}