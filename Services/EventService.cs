using Domain;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;
using System.IO.Compression;

namespace SEM.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly INotificationService _notificationService;

    public EventService(
        IEventRepository eventRepository,
        IUserProfileRepository userProfileRepository,
        INotificationService notificationService)
    {
        _eventRepository = eventRepository;
        _userProfileRepository = userProfileRepository;
        _notificationService = notificationService;
    }

    public async Task<ServiceResult<Event>> CreateEventAsync(EventRequest request, Guid modId)
    {
        var modUser = await _userProfileRepository.GetByIdAsync(modId);
        if (modUser.UserPrivilege == UserPrivilege.COMMON)
        {
            return ServiceResult<Event>.Fail("Вы не обладаете привилегиями для создания мероприятия");
        }
        if (!EventColors.AllowedColors.Contains(request.Color))
        {
            return ServiceResult<Event>.Fail("Недопустимый цвет мероприятия");
        }

        if (request.Publish && (request.Types == null || request.Types.Count == 0))
            return ServiceResult<Event>.Fail("Укажите хотя бы один тип мероприятия для публикации");

        if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 4096)
            return ServiceResult<Event>.Fail("Описание не должно превышать 4096 символов");

        request.Categories ??= new List<string>();
        request.Participants ??= new List<EventParticipantAssignmentDto>();
        request.ResponsiblePersonId = modId;
        var bufferDays = request.BufferDays ?? 14;
        if (bufferDays is < 7 or > 21)
            return ServiceResult<Event>.Fail("Переходный буфер должен быть от 7 до 21 дней");
        request.BufferDays = bufferDays;

        var createdEvent = await _eventRepository.AddEventAsync(request);

        if (request.Publish)
            await NotifyEventPublishedOnCreateAsync(createdEvent, request.Categories ?? new List<string>());

        return ServiceResult<Event>.Ok(createdEvent);
    }

    private async Task NotifyEventPublishedOnCreateAsync(Event evt, IReadOnlyCollection<string> categories)
    {
        var recipients = await _eventRepository.GetPublicationSubscribersAsync(evt.ResponsiblePersonId, categories, evt.Id);
        foreach (var userId in recipients.Distinct())
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = "EventPublished",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    event_id = evt.Id,
                    event_name = evt.Name,
                    start_at = evt.StartDate
                }),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationService.AddNotificationIfEnabledAsync(notification);
        }
    }

    public async Task<ServiceResult<EventResponse>> GetEventByIdAsync(Guid eventId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<EventResponse>.Fail("Мероприятие не найдено");

        var participants = _event.EventRoles?
            .Where(er => er.User != null)
            .Select(er => new UserResponse()
            {
                Id = er.User.Id,
                LastName = er.User.LastName,
                FirstName = er.User.FirstName,
                Profession = er.User.Profession,
                AvatarUrl = er.User.AvatarUrl,
                IsContact = er.IsContact
            })
            .Take(7)
            .ToList()
        ?? new List<UserResponse>();
        var response = new EventResponse
        {
            Id = _event.Id,
            Name = _event.Name ?? string.Empty,
            Description = _event.Description ?? string.Empty,
            StartDate = _event.StartDate,
            EndDate = _event.EndDate,
            Location = _event.Location,
            Auditorium = _event.Auditorium,
            VenueFormat = _event.VenueFormat,
            Types = _event.SelectedTypes?.Select(t => t.TypeKind).ToList() ?? new List<EventTypeKind>(),
            ResponsiblePersonId = _event.ResponsiblePersonId,
            MaxParticipants = _event.MaxParticipants,
            Color = _event.Color,
            Avatar = _event.Avatar ?? string.Empty,
            Categories = _event.EventCategories?.Select(ec => ec.Category.Name).ToList() ?? new List<string>(),
            PreviewPhotos = _event.Photos?.Select(p => p.FilePath).ToList() ?? new List<string>(),
            Participants = participants,
            ParticipantsCount = participants.Count,
            LifecycleState = _event.LifecycleState
            ,IsCancelled = _event.IsCancelled
            ,BufferDays = _event.BufferDays
        };

        return ServiceResult<EventResponse>.Ok(response);
    }

    public async Task<ServiceResult<List<Event>>> SearchEventsAsync(SearchRequest request)
    {
        var events = await _eventRepository.SearchEventsAsync(request);
        return ServiceResult<List<Event>>.Ok(events);
    }

    public async Task<ServiceResult<List<Event>>> SearchArchivedEventsAsync(SearchRequest request, Guid userId)
    {
        var events = await _eventRepository.SearchArchivedEventsAsync(request, userId);
        var visible = events.Where(e =>
        {
            if (e.ResponsiblePersonId == userId)
                return true;
            var role = e.EventRoles.FirstOrDefault(r => r.UserId == userId)?.ParticipantRole;
            return role == ParticipantRoleKind.Editor || role == ParticipantRoleKind.Organizer;
        }).ToList();
        return ServiceResult<List<Event>>.Ok(visible);
    }

    public async Task<ServiceResult<List<CategoryResponse>>> GetEventCategoriesAsync(Guid eventId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<List<CategoryResponse>>.Fail("Мероприятие не найдено");
        var categories = await _eventRepository.GetEventCategoriesAsync(eventId);
        return ServiceResult<List<CategoryResponse>>.Ok(categories);
    }
    
    public async Task<ServiceResult<List<Event>>> GetMyEventsAsync(Guid userId)
    {
        var events = await _eventRepository.GetMyEventsAsync(userId);
        return ServiceResult<List<Event>>.Ok(events);
    }

    public async Task<ServiceResult<List<Category>>> GetAllCategoriesAsync()
    {
        var categories = await _eventRepository.GetAllCategoriesAsync();
        return ServiceResult<List<Category>>.Ok(categories);
    }

    public async Task<ServiceResult<EventCategory>> AddCategoryToEventAsync(Guid eventId, string categoryName, Guid userId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<EventCategory>.Fail("Мероприятие не найдено");
        if (_event.ResponsiblePersonId != userId)
            return ServiceResult<EventCategory>.Fail("Вы не являетесь владельцем мероприятия");
        
        var newCategory = await _eventRepository.AddCategoryToEventAsync(eventId, categoryName);
        return ServiceResult<EventCategory>.Ok(newCategory);
    }
    
    public async Task<ServiceResult<bool>> DeleteCategoryInEventAsync(Guid eventId, Guid categoryId, Guid userId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");
        if (_event.ResponsiblePersonId != userId)
            return ServiceResult<bool>.Fail("Вы не являетесь владельцем мероприятия");
        
        await _eventRepository.DeleteCategoryInEventAsync(eventId, categoryId);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteEventAsync(Guid eventId, Guid userId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");
        if (_event.ResponsiblePersonId != userId)
            return ServiceResult<bool>.Fail("Вы не являетесь владельцем мероприятия");
        if (!CanDeleteEvent(_event))
            return ServiceResult<bool>.Fail("Удаление доступно только для мероприятий до даты начала и не в архиве");

        await _eventRepository.DeleteEventAndUnusedCategoriesAsync(_event);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> AddSuscriberAsync(Guid eventId, Guid userId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");
        if (!CanModifyParticipants(_event))
            return ServiceResult<bool>.Fail("Изменение состава участников недоступно после начала мероприятия");

        await _eventRepository.AddSuscriberAsync(eventId, userId);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteSuscriber(Guid eventId, Guid userId, Guid? transferToUserId = null)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");
        if (!CanModifyParticipants(_event))
            return ServiceResult<bool>.Fail("Изменение состава участников недоступно после начала мероприятия");

        if (_event.ResponsiblePersonId == userId)
        {
            var editors = _event.EventRoles
                .Where(r => r.UserId != userId && r.ParticipantRole == ParticipantRoleKind.Editor)
                .Select(r => r.UserId)
                .Distinct()
                .ToList();

            if (editors.Count == 0)
                return ServiceResult<bool>.Fail("Организатор не может выйти: в мероприятии нет редакторов для передачи роли");

            if (!transferToUserId.HasValue)
                return ServiceResult<bool>.Fail("Для выхода организатора передайте transferToUserId (id редактора)");

            if (transferToUserId.Value == userId)
                return ServiceResult<bool>.Fail("Нельзя передать роль организатора самому себе");

            if (!editors.Contains(transferToUserId.Value))
                return ServiceResult<bool>.Fail("Передать роль организатора можно только участнику с ролью Editor");

            _event.ResponsiblePersonId = transferToUserId.Value;
            await _eventRepository.UpdateEventAsync(_event);
            await _eventRepository.SetParticipantRoleForUser(eventId, transferToUserId.Value, ParticipantRoleKind.Organizer);
        }

        await _eventRepository.DeleteSuscriber(eventId, userId);
        return ServiceResult<bool>.Ok(true);
    }
    
    public async Task<ServiceResult<bool>> DeleteByOrganizerSuscriber(Guid eventId, Guid userId, Guid organizerId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");
        if (!CanModifyParticipants(_event))
            return ServiceResult<bool>.Fail("Изменение состава участников недоступно после начала мероприятия");
        if (_event.ResponsiblePersonId != organizerId)
            return ServiceResult<bool>.Fail("Вы не являетесь создателем мероприятия");
        if (userId == organizerId)
            return ServiceResult<bool>.Fail("Вы организатор, вы не можете удалить себя");

        await _eventRepository.DeleteSuscriberByOrganizer(eventId, userId);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<EventRole>> SetParticipantRoleForUserAsync(
        Guid eventId, Guid userId, ParticipantRoleKind role, Guid currentUserId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<EventRole>.Fail("Мероприятие не найдено");

        if (_event.ResponsiblePersonId != currentUserId)
            return ServiceResult<EventRole>.Fail("Вы не являетесь создателем мероприятия");
        if (!CanModifyParticipants(_event))
            return ServiceResult<EventRole>.Fail("Изменение состава участников недоступно после начала мероприятия");

        if (role == ParticipantRoleKind.Organizer && userId != _event.ResponsiblePersonId)
            return ServiceResult<EventRole>.Fail("Роль «Организатор» закреплена за владельцем мероприятия");

        var subscribers = await _eventRepository.GetAllSuscribersWithoutOffsetAsync(eventId, null, null);
        if (userId != _event.ResponsiblePersonId && !subscribers.Any(u => u.id == userId))
            return ServiceResult<EventRole>.Fail("Нельзя назначить роль пользователю, который не является участником мероприятия");

        var row = await _eventRepository.SetParticipantRoleForUser(eventId, userId, role);
        return ServiceResult<EventRole>.Ok(row);
    }

    public async Task<ServiceResult<List<EventFixedRoleInfoDto>>> GetRolesByEvent(Guid eventId, int count, int offset)
    {
        var roles = await _eventRepository.GetFixedRolesForEventAsync(eventId, count, offset);
        return ServiceResult<List<EventFixedRoleInfoDto>>.Ok(roles);
    }

    public async Task<ServiceResult<EventUserAndCountResponse>> GetAllSuscribersAsync(Guid eventId, string? name, string? role, int count, int offset)
    {
        var subs = await _eventRepository.GetAllSuscribersAsync(eventId, name, role, count, offset);
        return ServiceResult<EventUserAndCountResponse>.Ok(subs);
    }
    
    public async Task<ServiceResult<List<EventUserResponse>>> GetAllSuscribersWithoutOffsetAsync(Guid eventId, string? name, string? role)
    {
        var subs = await _eventRepository.GetAllSuscribersWithoutOffsetAsync(eventId, name, role);
        return ServiceResult<List<EventUserResponse>>.Ok(subs);
    }
    
    public async Task<ServiceResult<List<AssigneeCandidateDto>>> GetAssigneeCandidatesAsync(Guid eventId, Guid userId)
    {
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<List<AssigneeCandidateDto>>.Fail("Мероприятие не найдено");

        var isParticipant = userId == evt.ResponsiblePersonId || evt.EventRoles.Any(r => r.UserId == userId);
        if (!isParticipant)
            return ServiceResult<List<AssigneeCandidateDto>>.Fail("Вы не являетесь участником мероприятия");

        var candidates = evt.EventRoles
            .Where(r => r.User != null)
            .Where(r => r.ParticipantRole is ParticipantRoleKind.Editor or ParticipantRoleKind.Assistant)
            .Select(r => new AssigneeCandidateDto
            {
                Id = r.UserId,
                Name = $"{r.User.LastName} {r.User.FirstName}".Trim(),
                Profession = r.User.Profession,
                AvatarUrl = r.User.AvatarUrl,
                Role = r.ParticipantRole.ToString()
            })
            .OrderBy(x => x.Name)
            .ToList();

        return ServiceResult<List<AssigneeCandidateDto>>.Ok(candidates);
    }


    public async Task<ServiceResult<Event>> UpdateEventAsync(Guid eventId, EventUpdateRequest request, Guid userId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<Event>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != userId)
            return ServiceResult<Event>.Fail("Вы не являетесь создателем мероприятия");
        
        if (curEvent.LifecycleState is EventLifecycleState.Completed or EventLifecycleState.Archived)
            return ServiceResult<Event>.Fail("Мероприятие завершено");

        curEvent.Name = request.Name ?? curEvent.Name;
        curEvent.Description = request.Description ?? curEvent.Description;
        if (request.StartDate.HasValue)
            curEvent.StartDate = request.StartDate.Value;
        curEvent.EndDate = request.EndDate ?? curEvent.EndDate;
        curEvent.Location = request.Location ?? curEvent.Location;
        curEvent.Auditorium = request.Auditorium ?? curEvent.Auditorium;
        if (request.VenueFormat.HasValue)
            curEvent.VenueFormat = request.VenueFormat.Value;
        curEvent.MaxParticipants = request.MaxParticipants ?? curEvent.MaxParticipants;
        curEvent.Color = request.Color ?? curEvent.Color;

        var updatedEvent = await _eventRepository.UpdateEventAsync(curEvent);
        if (request.Types != null)
            await _eventRepository.ReplaceEventTypesAsync(eventId, request.Types);
        return ServiceResult<Event>.Ok(updatedEvent);
    }

    public async Task<ServiceResult<Event>> UpdateEventLifecycleStateAsync(Guid eventId, EventLifecycleUpdateRequest request, Guid userId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<Event>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != userId)
            return ServiceResult<Event>.Fail("Вы не являетесь создателем мероприятия");

        var validationError = ValidateLifecycleTransition(curEvent, request.LifecycleState);
        if (validationError != null)
            return ServiceResult<Event>.Fail(validationError);

        var previousState = curEvent.LifecycleState;
        curEvent.LifecycleState = request.LifecycleState;
        if (request.LifecycleState != EventLifecycleState.Cancelled)
            curEvent.IsCancelled = false;
        var updatedEvent = await _eventRepository.UpdateEventAsync(curEvent);

        if (previousState != EventLifecycleState.Published && request.LifecycleState == EventLifecycleState.Published)
        {
            await NotifyEventPublishedAsync(updatedEvent);
        }
        return ServiceResult<Event>.Ok(updatedEvent);
    }

    public async Task<ServiceResult<Event>> SetEventCancellationAsync(Guid eventId, EventCancellationRequest request, Guid userId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<Event>.Fail("Мероприятие не найдено");
        if (curEvent.ResponsiblePersonId != userId)
            return ServiceResult<Event>.Fail("Вы не являетесь создателем мероприятия");
        if (curEvent.LifecycleState != EventLifecycleState.Published)
            return ServiceResult<Event>.Fail("Отмена доступна только для мероприятия в статусе Published");

        curEvent.IsCancelled = request.IsCancelled;
        curEvent.CancelledAt = request.IsCancelled ? DateTime.UtcNow : null;
        curEvent.LifecycleState = request.IsCancelled ? EventLifecycleState.Cancelled : EventLifecycleState.Published;
        var updatedEvent = await _eventRepository.UpdateEventAsync(curEvent);

        if (request.IsCancelled)
            await NotifyEventCancelledAsync(curEvent);

        return ServiceResult<Event>.Ok(updatedEvent);
    }

    public async Task<ServiceResult<List<PhotoResponse>>> GetEventPhotosAsync(Guid eventId, int count, int offset)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<List<PhotoResponse>>.Fail("Мероприятие не найдено");

        var photos = await _eventRepository.GetEventPhotosAsync(eventId, offset, count);
        return ServiceResult<List<PhotoResponse>>.Ok(photos);
    }
    
    public async Task<ServiceResult<string>> AddEventPhotoAsync(Guid eventId, IFormFile file)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<string>.Fail("Мероприятие не найдено");

        if (file == null || file.Length == 0)
            return ServiceResult<string>.Fail("Файл не загружен");

        if (!IsAllowedMedia(file))
            return ServiceResult<string>.Fail("Разрешены только файлы фото и видео");

        if (!CanUploadEventMedia(_event))
            return ServiceResult<string>.Fail("Загрузка медиа доступна только после начала и до архивации мероприятия");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = "/" + Path.Combine("uploads", fileName).Replace("\\", "/");
        await _eventRepository.AddEventPhotoAsync(new EventPhoto { Id = Guid.NewGuid(), EventId = eventId, FilePath = relativePath });

        return ServiceResult<string>.Ok(relativePath);
    }

    private static bool IsAllowedMedia(IFormFile file)
    {
        var contentType = file.ContentType?.Trim();
        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();

        var byContentType =
            !string.IsNullOrWhiteSpace(contentType) &&
            (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
             contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase));

        // Бэкап-проверка по расширению, чтобы не принимать произвольные файлы при пустом/битом ContentType.
        var byExtension = ext is
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" or ".heic" or ".heif" or
            ".mp4" or ".mov" or ".m4v" or ".webm" or ".avi" or ".mkv";

        return byContentType && byExtension;
    }

    public async Task<ServiceResult<bool>> AddContact(Guid eventId, Guid userId, Guid currentUserId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != currentUserId)
            return ServiceResult<bool>.Fail("Вы не являетесь создателем мероприятия");
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event.LifecycleState == EventLifecycleState.Completed)
            return ServiceResult<bool>.Fail("Мероприятие завершено");
        await _eventRepository.AddContact(eventId, userId);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<List<ContactResponse>>> GetContacts(Guid eventId)
    {
        var contacts = await _eventRepository.GetContacts(eventId);
        return ServiceResult<List<ContactResponse>>.Ok(contacts);
    }

    public async Task<ServiceResult<Event>> FinishEventAsync(Guid eventId, Guid userId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<Event>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != userId)
            return ServiceResult<Event>.Fail("Вы не являетесь создателем мероприятия");
        
        curEvent.LifecycleState = EventLifecycleState.Completed;
        var updatedEvent = await _eventRepository.FinishEventAsync(curEvent);
        return ServiceResult<Event>.Ok(updatedEvent);
    }

    public async Task<ServiceResult<string>> GetEventPhotoByIdAsync(Guid eventId, Guid photoId)
    {
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event == null)
            return ServiceResult<string>.Fail("Мероприятие не найдено");
        var res = await _eventRepository.GetEventPhotoByIdAsync(eventId, photoId);
        if (res == null)
            return ServiceResult<string>.Ok("Фото не найдено");
        return ServiceResult<string>.Ok(res);
    }
    
    public async Task<ServiceResult<DownloadMediaResult>> DownloadEventPhotoAsync(Guid eventId, Guid photoId)
    {
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<DownloadMediaResult>.Fail("Мероприятие не найдено");

        var photo = await _eventRepository.GetEventPhotoEntityAsync(eventId, photoId);
        if (photo == null || string.IsNullOrWhiteSpace(photo.FilePath))
            return ServiceResult<DownloadMediaResult>.Fail("Файл не найден");

        // FilePath хранится как "/uploads/<guid>.<ext>"
        var relative = photo.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);
        if (!File.Exists(fullPath))
            return ServiceResult<DownloadMediaResult>.Fail("Файл не найден");

        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".m4v" => "video/x-m4v",
            ".webm" => "video/webm",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            _ => "application/octet-stream"
        };

        var bytes = await File.ReadAllBytesAsync(fullPath);
        var fileName = Path.GetFileName(fullPath);
        return ServiceResult<DownloadMediaResult>.Ok(new DownloadMediaResult
        {
            Bytes = bytes,
            ContentType = contentType,
            FileName = fileName
        });
    }
    
    public async Task<ServiceResult<DownloadMediaResult>> DownloadEventPhotosArchiveAsync(Guid eventId, IReadOnlyCollection<Guid> photoIds)
    {
        if (photoIds == null || photoIds.Count == 0)
            return ServiceResult<DownloadMediaResult>.Fail("Список файлов пуст");

        // Базовая защита от слишком больших запросов.
        if (photoIds.Count > 50)
            return ServiceResult<DownloadMediaResult>.Fail("Слишком много файлов для скачивания за один раз");

        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<DownloadMediaResult>.Fail("Мероприятие не найдено");

        var photos = await _eventRepository.GetEventPhotoEntitiesAsync(eventId, photoIds);
        if (photos.Count == 0)
            return ServiceResult<DownloadMediaResult>.Fail("Файлы не найдены");

        await using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var photo in photos)
            {
                if (string.IsNullOrWhiteSpace(photo.FilePath))
                    continue;

                var relative = photo.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);
                if (!File.Exists(fullPath))
                    continue;

                var entryName = Path.GetFileName(fullPath);
                // на случай коллизий имён
                if (zip.GetEntry(entryName) != null)
                    entryName = $"{photo.Id}{Path.GetExtension(fullPath)}";

                var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await using var fileStream = File.OpenRead(fullPath);
                await fileStream.CopyToAsync(entryStream);
            }
        }

        var bytes = ms.ToArray();
        if (bytes.Length == 0)
            return ServiceResult<DownloadMediaResult>.Fail("Не удалось собрать архив");

        return ServiceResult<DownloadMediaResult>.Ok(new DownloadMediaResult
        {
            Bytes = bytes,
            ContentType = "application/zip",
            FileName = $"event-{eventId}-media.zip"
        });
    }

    public async Task<ServiceResult<bool>> DeleteEventPhotoAsync(Guid eventId, Guid photoId, Guid userId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != userId)
            return ServiceResult<bool>.Fail("Вы не являетесь создателем мероприятия");
        if (curEvent.LifecycleState == EventLifecycleState.Archived)
            return ServiceResult<bool>.Fail("Мероприятие в архиве");
        if (curEvent.LifecycleState == EventLifecycleState.Completed)
            return ServiceResult<bool>.Fail("В переходном буфере удалять медиа может только организатор");
        await _eventRepository.DeleteEventPhotoAsync(eventId, photoId);
        return ServiceResult<bool>.Ok(true);
    }
    
    public async Task<ServiceResult<bool>> DeleteContact(Guid eventId, Guid userId, Guid currentUserId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != currentUserId)
            return ServiceResult<bool>.Fail("Вы не являетесь создателем мероприятия");
        var _event = await _eventRepository.GetEventByIdAsync(eventId);
        if (_event.LifecycleState == EventLifecycleState.Completed)
            return ServiceResult<bool>.Fail("Мероприятие завершено");
        if (userId == curEvent.ResponsiblePersonId)
            return ServiceResult<bool>.Fail("Мероприятие не может остаться без создателя/организатора");
        await _eventRepository.DeleteContact(eventId, userId);
        return ServiceResult<bool>.Ok(true);
    }
    
    public async Task<ServiceResult<bool>> DeleteEventPhotosAsync(Guid eventId, List<Guid> photoIds, Guid userId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<bool>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != userId)
            return ServiceResult<bool>.Fail("Вы не являетесь создателем мероприятия");
        if (curEvent.LifecycleState == EventLifecycleState.Archived)
            return ServiceResult<bool>.Fail("Мероприятие в архиве");
        if (curEvent.LifecycleState == EventLifecycleState.Completed)
            return ServiceResult<bool>.Fail("В переходном буфере удалять медиа может только организатор");

        await _eventRepository.DeleteEventPhotosAsync(eventId, photoIds);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<Event>> CopyArchivedEventAsTemplateAsync(Guid sourceEventId, Guid userId, string? name)
    {
        var user = await _userProfileRepository.GetByIdAsync(userId);
        if (user == null || user.UserPrivilege == UserPrivilege.COMMON)
            return ServiceResult<Event>.Fail("Недостаточно прав для копирования архивного мероприятия");

        var source = await _eventRepository.GetEventByIdAsync(sourceEventId);
        if (source == null)
            return ServiceResult<Event>.Fail("Исходное мероприятие не найдено");
        if (source.LifecycleState != EventLifecycleState.Archived)
            return ServiceResult<Event>.Fail("Копирование как шаблон доступно только для архивного мероприятия");

        var isParticipant = source.ResponsiblePersonId == userId || source.EventRoles.Any(r => r.UserId == userId);
        var role = source.EventRoles.FirstOrDefault(r => r.UserId == userId)?.ParticipantRole;
        if (!isParticipant || (role != null && role is ParticipantRoleKind.Assistant or ParticipantRoleKind.Observer))
            return ServiceResult<Event>.Fail("Недостаточно прав для доступа к архиву мероприятия");

        var copied = await _eventRepository.CloneArchivedEventAsTemplateAsync(source, userId, string.IsNullOrWhiteSpace(name) ? $"{source.Name} (копия)" : name.Trim());
        return ServiceResult<Event>.Ok(copied);
    }
    
    public async Task<ServiceResult<string>> UploadEventAvatarAsync(Guid eventId, IFormFile avatar, Guid userId)
    {
        var curEvent = await _eventRepository.GetEventByIdAsync(eventId);
        if (curEvent == null)
            return ServiceResult<string>.Fail("Мероприятие не найдено");

        if (curEvent.ResponsiblePersonId != userId)
            return ServiceResult<string>.Fail("Вы не являетесь создателем мероприятия");

        if (curEvent.LifecycleState == EventLifecycleState.Completed)
            return ServiceResult<string>.Fail("Мероприятие завершено");

        if (avatar == null || avatar.Length == 0)
            return ServiceResult<string>.Fail("Файл не загружен");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "event-avatars");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await avatar.CopyToAsync(stream);
        }

        var relativePath = $"/event-avatars/{fileName}";
        
        if (!string.IsNullOrEmpty(curEvent.Avatar))
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", curEvent.Avatar.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (File.Exists(oldPath))
                File.Delete(oldPath);
        }

        curEvent.Avatar = relativePath;
        await _eventRepository.UpdateAvatarEventAsync(curEvent);

        return ServiceResult<string>.Ok(relativePath);
    }

    private static bool CanModifyParticipants(Event evt)
    {
        if (evt.LifecycleState is EventLifecycleState.Completed or EventLifecycleState.Archived)
            return false;
        return DateTime.UtcNow < evt.StartDate;
    }

    private static bool CanDeleteEvent(Event evt)
    {
        if (evt.LifecycleState == EventLifecycleState.Archived)
            return false;
        return DateTime.UtcNow < evt.StartDate;
    }

    private static bool CanUploadEventMedia(Event evt)
    {
        if (evt.LifecycleState == EventLifecycleState.Archived)
            return false;
        return DateTime.UtcNow >= evt.StartDate;
    }

    private static string? ValidateLifecycleTransition(Event evt, EventLifecycleState next)
    {
        var now = DateTime.UtcNow;
        var hasStarted = now >= evt.StartDate;

        if (evt.LifecycleState == EventLifecycleState.Archived && next != EventLifecycleState.Archived)
            return "Архивный статус необратим";

        return (evt.LifecycleState, next) switch
        {
            (EventLifecycleState.Draft, EventLifecycleState.Published) => null,
            (EventLifecycleState.Published, EventLifecycleState.Completed) => null,
            (EventLifecycleState.Published, EventLifecycleState.Cancelled) => null,
            (EventLifecycleState.Published, EventLifecycleState.Draft) when !hasStarted => null,
            (EventLifecycleState.Completed, EventLifecycleState.Archived) => null,
            (EventLifecycleState.Cancelled, EventLifecycleState.Completed) => null,
            (EventLifecycleState.Cancelled, EventLifecycleState.Published) => null,
            _ when evt.LifecycleState == next => null,
            _ => "Недопустимый переход статуса мероприятия"
        };
    }

    private async Task NotifyEventCancelledAsync(Event evt)
    {
        var subscribers = await _eventRepository.GetAllSuscribersWithoutOffsetAsync(evt.Id, null, null);
        var recipients = new HashSet<Guid>(subscribers.Select(s => s.id));
        recipients.Add(evt.ResponsiblePersonId);

        foreach (var userId in recipients)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = "EventCancelled",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    event_id = evt.Id,
                    event_name = evt.Name,
                    cancelled_at = evt.CancelledAt
                }),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationService.AddNotificationIfEnabledAsync(notification);
        }
    }

    private async Task NotifyEventPublishedAsync(Event evt)
    {
        var categories = evt.EventCategories.Select(c => c.Category.Name).ToList();
        var recipients = await _eventRepository.GetPublicationSubscribersAsync(evt.ResponsiblePersonId, categories, evt.Id);
        foreach (var userId in recipients.Distinct())
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = "EventPublished",
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    event_id = evt.Id,
                    event_name = evt.Name,
                    start_at = evt.StartDate
                }),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationService.AddNotificationIfEnabledAsync(notification);
        }
    }
}