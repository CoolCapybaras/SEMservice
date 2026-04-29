using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

public class EventAttachment
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EventId { get; set; }
    [JsonIgnore]
    public Event Event { get; set; } = null!;

    [Required]
    public Guid AuthorId { get; set; }
    [JsonIgnore]
    public User Author { get; set; } = null!;

    [Required]
    public EventAttachmentKind Kind { get; set; }

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = null!;

    [Required]
    public string Resource { get; set; } = null!;

    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? Size { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum EventAttachmentKind
{
    File,
    Link
}