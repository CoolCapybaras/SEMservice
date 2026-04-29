using Domain;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.Services;

public class EventAttachmentService : IEventAttachmentService
{
    private readonly IEventAttachmentRepository _attachmentRepository;
    private readonly IEventRepository _eventRepository;

    public EventAttachmentService(IEventAttachmentRepository attachmentRepository, IEventRepository eventRepository)
    {
        _attachmentRepository = attachmentRepository;
        _eventRepository = eventRepository;
    }

    public async Task<ServiceResult<EventAttachmentResponse>> UploadFileAsync(Guid eventId, Guid userId, IFormFile file, string? title)
    {
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<EventAttachmentResponse>.Fail("Мероприятие не найдено");
        if (!CanManageAttachments(evt, userId))
            return ServiceResult<EventAttachmentResponse>.Fail("Недостаточно прав для загрузки документов");
        if (evt.LifecycleState == EventLifecycleState.Archived)
            return ServiceResult<EventAttachmentResponse>.Fail("Архивное мероприятие доступно только для чтения");
        if (file == null || file.Length == 0)
            return ServiceResult<EventAttachmentResponse>.Fail("Файл не загружен");
        if (file.Length > 50 * 1024 * 1024)
            return ServiceResult<EventAttachmentResponse>.Fail("Размер файла превышает 50 МБ");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "event-attachments", eventId.ToString());
        Directory.CreateDirectory(uploadsFolder);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = "/" + Path.Combine("event-attachments", eventId.ToString(), fileName).Replace("\\", "/");
        var entity = new EventAttachment
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            AuthorId = userId,
            Kind = EventAttachmentKind.File,
            Title = string.IsNullOrWhiteSpace(title) ? file.FileName : title.Trim(),
            Resource = relativePath,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            Size = file.Length
        };
        await _attachmentRepository.AddAsync(entity);
        return ServiceResult<EventAttachmentResponse>.Ok(ToResponse(entity));
    }

    public async Task<ServiceResult<EventAttachmentResponse>> AddLinkAsync(Guid eventId, Guid userId, EventAttachmentLinkRequest request)
    {
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<EventAttachmentResponse>.Fail("Мероприятие не найдено");
        if (!CanManageAttachments(evt, userId))
            return ServiceResult<EventAttachmentResponse>.Fail("Недостаточно прав для добавления ссылки");
        if (evt.LifecycleState == EventLifecycleState.Archived)
            return ServiceResult<EventAttachmentResponse>.Fail("Архивное мероприятие доступно только для чтения");
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Url))
            return ServiceResult<EventAttachmentResponse>.Fail("Заполните title и url");
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            return ServiceResult<EventAttachmentResponse>.Fail("Некорректный URL");

        var entity = new EventAttachment
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            AuthorId = userId,
            Kind = EventAttachmentKind.Link,
            Title = request.Title.Trim(),
            Resource = request.Url.Trim()
        };
        await _attachmentRepository.AddAsync(entity);
        return ServiceResult<EventAttachmentResponse>.Ok(ToResponse(entity));
    }

    public async Task<ServiceResult<List<EventAttachmentResponse>>> GetByEventAsync(Guid eventId, Guid userId)
    {
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<List<EventAttachmentResponse>>.Fail("Мероприятие не найдено");
        if (!IsParticipant(evt, userId))
            return ServiceResult<List<EventAttachmentResponse>>.Fail("Вы не являетесь участником мероприятия");

        var items = await _attachmentRepository.GetByEventIdAsync(eventId);
        return ServiceResult<List<EventAttachmentResponse>>.Ok(items.Select(ToResponse).ToList());
    }

    public async Task<ServiceResult<EventAttachmentResponse>> GetByIdAsync(Guid eventId, Guid attachmentId, Guid userId)
    {
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<EventAttachmentResponse>.Fail("Мероприятие не найдено");
        if (!IsParticipant(evt, userId))
            return ServiceResult<EventAttachmentResponse>.Fail("Вы не являетесь участником мероприятия");

        var item = await _attachmentRepository.GetByIdAsync(attachmentId);
        if (item == null || item.EventId != eventId)
            return ServiceResult<EventAttachmentResponse>.Fail("Вложение не найдено");
        return ServiceResult<EventAttachmentResponse>.Ok(ToResponse(item));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid eventId, Guid attachmentId, Guid userId)
    {
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");
        if (evt.LifecycleState == EventLifecycleState.Archived)
            return ServiceResult<bool>.Fail("Архивное мероприятие доступно только для чтения");

        var item = await _attachmentRepository.GetByIdAsync(attachmentId);
        if (item == null || item.EventId != eventId)
            return ServiceResult<bool>.Fail("Вложение не найдено");

        var role = GetRole(evt, userId);
        var canDelete = userId == evt.ResponsiblePersonId || ((role == ParticipantRoleKind.Editor || role == ParticipantRoleKind.Assistant) && item.AuthorId == userId);
        if (!canDelete)
            return ServiceResult<bool>.Fail("Недостаточно прав для удаления вложения");

        if (item.Kind == EventAttachmentKind.File && !string.IsNullOrWhiteSpace(item.Resource))
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", item.Resource.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        await _attachmentRepository.DeleteAsync(item);
        return ServiceResult<bool>.Ok(true);
    }

    private static EventAttachmentResponse ToResponse(EventAttachment entity) => new()
    {
        Id = entity.Id,
        EventId = entity.EventId,
        AuthorId = entity.AuthorId,
        Kind = entity.Kind,
        Title = entity.Title,
        Resource = entity.Resource,
        OriginalFileName = entity.OriginalFileName,
        ContentType = entity.ContentType,
        Size = entity.Size,
        CreatedAt = entity.CreatedAt
    };

    private static bool IsParticipant(Event evt, Guid userId) => userId == evt.ResponsiblePersonId || evt.EventRoles.Any(r => r.UserId == userId);
    private static bool CanManageAttachments(Event evt, Guid userId)
    {
        if (userId == evt.ResponsiblePersonId)
            return true;
        var role = GetRole(evt, userId);
        return role == ParticipantRoleKind.Editor || role == ParticipantRoleKind.Assistant;
    }
    private static ParticipantRoleKind? GetRole(Event evt, Guid userId) => evt.EventRoles.FirstOrDefault(r => r.UserId == userId)?.ParticipantRole;
}
