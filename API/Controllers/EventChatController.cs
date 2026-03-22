using System.Security.Claims;
using Domain.DTO;
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
    
    /// <summary>
    /// Поиск сообщений по подстроке в тексте (без учёта регистра), от самого раннего к позднему.
    /// </summary>
    [HttpGet("messages/search")]
    [Authorize]
    public async Task<IActionResult> SearchMessages(Guid eventId, [FromQuery] string text, [FromQuery] int maxResults = 500)
    {
        var userId = GetUserIdFromToken();
        var result = await _chatService.SearchMessagesAsync(eventId, text, userId, maxResults);
        return result.Success
            ? Ok(new { result = result.Data })
            : BadRequest(new { error = result.Error });
    }

    public class SendMessageRequest
    {
        public string Text { get; set; } = string.Empty;
        
        public Guid? ReplyToMessageId { get; set; }
    }
    
    public class SendMessageWithFilesRequest
    {
        public string? Text { get; set; }
        public List<IFormFile>? Files { get; set; }
        public Guid? ReplyToMessageId { get; set; }
    }
    
    public class AddMessageAttachmentsForm
    {
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
        var result = await _chatService.AddMessageAsync(eventId, userId, request.Text, null, request.ReplyToMessageId);
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

        var result = await _chatService.AddMessageAsync(eventId, userId, request.Text ?? string.Empty, attachments, request.ReplyToMessageId);
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
    
    /// <summary>
    /// Редактировать своё сообщение: текст и/или удаление вложений по id. Если текст менять не нужно, можно не передавать
    /// </summary>
    [HttpPatch("messages/{messageId:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateMessage(Guid eventId, Guid messageId, [FromBody] UpdateChatMessageRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _chatService.UpdateMessageAsync(eventId, messageId, userId, request.Text, request.RemoveAttachmentIds);
        return result.Success
            ? Ok(new { result = result.Data })
            : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Добавить файлы к своему сообщению.
    /// </summary>
    [HttpPost("messages/{messageId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    [Authorize]
    public async Task<IActionResult> AddMessageAttachments(Guid eventId, Guid messageId, [FromForm] AddMessageAttachmentsForm form)
    {
        var userId = GetUserIdFromToken();
        var attachments = new List<EventChatAttachment>();
        if (form.Files is { Count: > 0 })
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "chat-attachments", eventId.ToString());
            Directory.CreateDirectory(uploadsFolder);

            foreach (var file in form.Files)
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

        var result = await _chatService.AddAttachmentsToMessageAsync(eventId, messageId, userId, attachments);
        return result.Success
            ? Ok(new { result = result.Data })
            : BadRequest(new { error = result.Error });
    }
    
    /// <summary>
    /// Удалить одно вложение у своего сообщения.
    /// </summary>
    [HttpDelete("messages/{messageId:guid}/attachments/{attachmentId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveMessageAttachment(Guid eventId, Guid messageId, Guid attachmentId)
    {
        var userId = GetUserIdFromToken();
        var result = await _chatService.RemoveAttachmentFromMessageAsync(eventId, messageId, userId, attachmentId);
        return result.Success
            ? Ok(new { result = result.Data })
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Удалить своё сообщение (вложения удаляются с диска).
    /// </summary>
    [HttpDelete("messages/{messageId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteMessage(Guid eventId, Guid messageId)
    {
        var userId = GetUserIdFromToken();
        var result = await _chatService.DeleteMessageAsync(eventId, messageId, userId);
        return result.Success
            ? Ok(new { success = true })
            : BadRequest(new { error = result.Error });
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

