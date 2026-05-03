using SEM.Domain.Models;

namespace Domain.DTO;

public class EventAttachmentResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid AuthorId { get; set; }
    public EventAttachmentKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? Size { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>Для списка: имя автора.</summary>
    public string? AuthorDisplayName { get; set; }

    public string? AuthorAvatarUrl { get; set; }

    /// <summary>Для файлов — расширение с точкой (.docx); для ссылок null.</summary>
    public string? FileExtension { get; set; }

    /// <summary>Для ссылок — ключ площадки (figma, google-docs…); для файлов null.</summary>
    public string? LinkSiteKey { get; set; }
}