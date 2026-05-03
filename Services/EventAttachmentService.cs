using System.Globalization;
using Domain;
using Domain.DTO;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using SEM.Domain.Interfaces;
using SEM.Domain.Models;

namespace SEM.Services;

public class EventAttachmentService : IEventAttachmentService
{
    private static readonly (string HostSuffix, string Key, string Label)[] KnownLinkSites =
    {
        ("figma.com", "figma", "Figma"),
        ("docs.google.com", "google-docs", "Google Docs"),
        ("drive.google.com", "google-drive", "Google Drive"),
        ("sheets.google.com", "google-sheets", "Google Таблицы"),
        ("notion.so", "notion", "Notion"),
        ("miro.com", "miro", "Miro"),
        ("youtube.com", "youtube", "YouTube"),
        ("youtu.be", "youtube", "YouTube"),
        ("dropbox.com", "dropbox", "Dropbox"),
        ("canva.com", "canva", "Canva"),
        ("vk.com", "vk", "VK"),
        ("t.me", "telegram", "Telegram"),
    };

    private static readonly Dictionary<string, string> ExtensionLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "Pdf",
        [".doc"] = "Word",
        [".docx"] = "Word",
        [".xls"] = "Excel",
        [".xlsx"] = "Excel",
        [".csv"] = "Excel",
        [".ppt"] = "PowerPoint",
        [".pptx"] = "PowerPoint",
        [".txt"] = "Текст",
        [".zip"] = "Архив",
        [".rar"] = "Архив",
        [".7z"] = "Архив",
        [".png"] = "Изображение",
        [".jpg"] = "Изображение",
        [".jpeg"] = "Изображение",
        [".gif"] = "Изображение",
        [".webp"] = "Изображение",
    };

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
        var saved = await _attachmentRepository.GetByIdAsync(entity.Id);
        return ServiceResult<EventAttachmentResponse>.Ok(ToResponse(saved!));
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
        var saved = await _attachmentRepository.GetByIdAsync(entity.Id);
        return ServiceResult<EventAttachmentResponse>.Ok(ToResponse(saved!));
    }

    public async Task<ServiceResult<List<EventAttachmentResponse>>> GetByEventAsync(Guid eventId, Guid userId, EventAttachmentListQuery? query)
    {
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<List<EventAttachmentResponse>>.Fail("Мероприятие не найдено");
        if (!IsParticipant(evt, userId))
            return ServiceResult<List<EventAttachmentResponse>>.Fail("Вы не являетесь участником мероприятия");

        var items = await _attachmentRepository.GetByEventIdAsync(eventId);
        var q = query ?? new EventAttachmentListQuery();
        var filtered = ApplyFilters(items, q).Select(ToResponse).ToList();
        SortAttachments(filtered, q.Sort ?? "Newest");
        return ServiceResult<List<EventAttachmentResponse>>.Ok(filtered);
    }

    public async Task<ServiceResult<EventAttachmentFacetsResponse>> GetFacetsAsync(Guid eventId, Guid userId)
    {
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt == null)
            return ServiceResult<EventAttachmentFacetsResponse>.Fail("Мероприятие не найдено");
        if (!IsParticipant(evt, userId))
            return ServiceResult<EventAttachmentFacetsResponse>.Fail("Вы не являетесь участником мероприятия");

        var items = await _attachmentRepository.GetByEventIdAsync(eventId);
        var extSet = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in items.Where(x => x.Kind == EventAttachmentKind.File))
        {
            var ext = NormalizeFileExtension(a.OriginalFileName ?? a.Title);
            if (string.IsNullOrEmpty(ext))
                continue;
            if (!extSet.ContainsKey(ext))
                extSet[ext] = ExtensionLabelFor(ext);
        }

        var linkSet = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in items.Where(x => x.Kind == EventAttachmentKind.Link))
        {
            var (key, label) = ResolveLinkSite(a.Resource);
            if (!linkSet.ContainsKey(key))
                linkSet[key] = label;
        }

        var authors = items
            .Select(a => a.Author)
            .Where(u => u != null)
            .GroupBy(u => u!.Id)
            .Select(g => g.First()!)
            .OrderBy(u => UserDisplayName(u), StringComparer.Create(new CultureInfo("ru-RU"), ignoreCase: true))
            .Select(u => new AttachmentAuthorFacet
            {
                Id = u.Id,
                DisplayName = UserDisplayName(u),
                AvatarUrl = u.AvatarUrl
            })
            .ToList();

        var response = new EventAttachmentFacetsResponse
        {
            FileExtensions = extSet
                .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
                .Select(p => new AttachmentExtensionFacet { Extension = p.Key, Label = p.Value })
                .ToList(),
            LinkSites = linkSet
                .OrderBy(p => p.Value, StringComparer.Create(new CultureInfo("ru-RU"), ignoreCase: true))
                .Select(p => new AttachmentLinkSiteFacet { SiteKey = p.Key, Label = p.Value })
                .ToList(),
            Authors = authors
        };

        return ServiceResult<EventAttachmentFacetsResponse>.Ok(response);
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

    private static EventAttachmentResponse ToResponse(EventAttachment entity)
    {
        string? fileExt = null;
        string? linkKey = null;
        if (entity.Kind == EventAttachmentKind.File)
            fileExt = NormalizeFileExtension(entity.OriginalFileName ?? entity.Title);
        else
            linkKey = ResolveLinkSite(entity.Resource).Key;

        return new EventAttachmentResponse
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
            CreatedAt = entity.CreatedAt,
            AuthorDisplayName = entity.Author != null ? UserDisplayName(entity.Author) : null,
            AuthorAvatarUrl = entity.Author?.AvatarUrl,
            FileExtension = fileExt,
            LinkSiteKey = linkKey
        };
    }

    private static List<EventAttachment> ApplyFilters(IReadOnlyList<EventAttachment> items, EventAttachmentListQuery q)
    {
        var kinds = ParseKinds(q.Kinds);
        var authorIds = ParseGuidList(q.AuthorIds);
        var extFilters = ParseExtensionFilters(q.Extensions);
        var linkSiteFilters = ParseTokens(q.LinkSites);

        IEnumerable<EventAttachment> seq = items;

        if (kinds.Count > 0)
            seq = seq.Where(a => kinds.Contains(a.Kind));

        if (authorIds.Count > 0)
            seq = seq.Where(a => authorIds.Contains(a.AuthorId));

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var needle = q.Q.Trim();
            seq = seq.Where(a =>
                a.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || (a.OriginalFileName?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false)
                || a.Resource.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        var extActive = extFilters.Count > 0;
        var linkActive = linkSiteFilters.Count > 0;
        if (extActive || linkActive)
        {
            seq = seq.Where(a =>
            {
                if (a.Kind == EventAttachmentKind.File && extActive)
                {
                    var ext = NormalizeFileExtension(a.OriginalFileName ?? a.Title);
                    return extFilters.Contains(ext);
                }

                if (a.Kind == EventAttachmentKind.Link && linkActive)
                {
                    var key = ResolveLinkSite(a.Resource).Key;
                    return linkSiteFilters.Contains(key);
                }

                return false;
            });
        }

        return seq.ToList();
    }

    private static void SortAttachments(List<EventAttachmentResponse> rows, string sort)
    {
        var mode = (sort ?? "Newest").Trim();
        switch (mode.ToLowerInvariant())
        {
            case "oldest":
                rows.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
                break;
            case "titleasc":
            case "a-z":
                rows.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase));
                break;
            case "authorasc":
                rows.Sort((a, b) => string.Compare(a.AuthorDisplayName ?? "", b.AuthorDisplayName ?? "", StringComparison.CurrentCultureIgnoreCase));
                break;
            default:
                rows.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
                break;
        }
    }

    private static HashSet<EventAttachmentKind> ParseKinds(string? raw)
    {
        var set = new HashSet<EventAttachmentKind>();
        if (string.IsNullOrWhiteSpace(raw))
            return set;
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<EventAttachmentKind>(part, ignoreCase: true, out var k))
                set.Add(k);
        }

        return set;
    }

    private static HashSet<Guid> ParseGuidList(string? raw)
    {
        var set = new HashSet<Guid>();
        if (string.IsNullOrWhiteSpace(raw))
            return set;
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(part, out var id))
                set.Add(id);
        }

        return set;
    }

    private static HashSet<string> ParseExtensionFilters(string? raw)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
            return set;
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var ext = part.StartsWith('.') ? part : "." + part;
            set.Add(ext.ToLowerInvariant());
        }

        return set;
    }

    private static HashSet<string> ParseTokens(string? raw)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
            return set;
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            set.Add(part.ToLowerInvariant());
        return set;
    }

    private static string NormalizeFileExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;
        var ext = Path.GetExtension(fileName);
        return string.IsNullOrEmpty(ext) ? string.Empty : ext.ToLowerInvariant();
    }

    private static string ExtensionLabelFor(string extensionWithDot)
    {
        return ExtensionLabels.TryGetValue(extensionWithDot, out var l)
            ? l
            : extensionWithDot.TrimStart('.').ToUpperInvariant();
    }

    private static (string Key, string Label) ResolveLinkSite(string resource)
    {
        if (!Uri.TryCreate(resource, UriKind.Absolute, out var uri))
            return ("other", "Ссылка");

        var host = uri.IdnHost.ToLowerInvariant();
        foreach (var (suffix, key, label) in KnownLinkSites)
        {
            if (host == suffix || host.EndsWith("." + suffix, StringComparison.Ordinal))
                return (key, label);
        }

        return ("other", uri.Host);
    }

    private static string UserDisplayName(User u)
    {
        var parts = new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim());
        var name = string.Join(" ", parts);
        return string.IsNullOrEmpty(name) ? (u.Email ?? u.Id.ToString()) : name;
    }

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
