using System.Security.Claims;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Models;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/chat")]
public class EventChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public EventChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Получить сообщения чата мероприятия
    /// </summary>
    [HttpGet("messages")]
    [Authorize]
    public async Task<IActionResult> GetMessages(Guid eventId, int count, int offset)
    {
        var userId = GetUserIdFromToken();
        var result = await _chatService.GetMessagesAsync(eventId, count, offset, userId);
        return result.Success
            ? Ok(new { result = result.Data })
            : BadRequest(new { error = result.Error });
    }

    public class SendMessageRequest
    {
        public string Text { get; set; } = string.Empty;
    }
    
    public class SendMessageWithFilesRequest
    {
        public string? Text { get; set; }
        public List<IFormFile>? Files { get; set; }
    }

    /// <summary>
    /// Отправить сообщение в чат мероприятия
    /// </summary>
    [HttpPost("messages")]
    [Authorize]
    public async Task<IActionResult> SendMessage(Guid eventId, [FromBody] SendMessageRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _chatService.AddMessageAsync(eventId, userId, request.Text);
        return result.Success
            ? Ok(new { result = result.Data })
            : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Отправить сообщение в чат мероприятия с файлами (опционально)
    /// </summary>
    [HttpPost("messages/with-files")]
    [Consumes("multipart/form-data")]
    [Authorize]
    public async Task<IActionResult> SendMessageWithFiles(Guid eventId, [FromForm] SendMessageWithFilesRequest request)
    {
        var userId = GetUserIdFromToken();

        var attachments = new List<EventChatAttachment>();
        if (request.Files is { Count: > 0 })
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "chat-attachments", eventId.ToString());
            Directory.CreateDirectory(uploadsFolder);

            foreach (var file in request.Files)
            {
                if (file == null || file.Length == 0)
                    continue;

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = "/" + Path.Combine("chat-attachments", eventId.ToString(), fileName).Replace("\\", "/");
                attachments.Add(new EventChatAttachment
                {
                    Id = Guid.NewGuid(),
                    FilePath = relativePath,
                    OriginalFileName = file.FileName,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    Size = file.Length,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        var result = await _chatService.AddMessageAsync(eventId, userId, request.Text ?? string.Empty, attachments);
        return result.Success
            ? Ok(new { result = result.Data })
            : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Скачать файл, прикреплённый к сообщению чата
    /// </summary>
    [HttpGet("attachments/{attachmentId:guid}/download")]
    [Authorize]
    public async Task<IActionResult> DownloadAttachment(Guid eventId, Guid attachmentId)
    {
        var userId = GetUserIdFromToken();
        var result = await _chatService.GetAttachmentForDownloadAsync(eventId, userId, attachmentId);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        var attachment = result.Data!;
        var relative = attachment.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);

        if (!System.IO.File.Exists(fullPath))
            return NotFound(new { error = "Файл не найден" });

        return PhysicalFile(fullPath, attachment.ContentType, attachment.OriginalFileName);
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

