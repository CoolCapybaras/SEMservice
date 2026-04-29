using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

public class Event
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public string? Name { get; set; }

    public string? Avatar { get; set; }

    [StringLength(4096)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = null!;

    [StringLength(200)]
    public string? Auditorium { get; set; }

    public VenueFormat VenueFormat { get; set; }

    public EventLifecycleState LifecycleState { get; set; } = EventLifecycleState.Draft;
    
    public bool IsCancelled { get; set; }
    
    public DateTime? CancelledAt { get; set; }
    
    /// <summary>Длина переходного буфера в днях после завершения (1-21).</summary>
    public int BufferDays { get; set; } = 14;

    public DateTime? EventStart24hNotificationSentAt { get; set; }
    public DateTime? EventStart1hNotificationSentAt { get; set; }
    public DateTime? BufferEnding3dNotificationSentAt { get; set; }

    public Guid ResponsiblePersonId { get; set; }

    [JsonIgnore]
    public User ResponsiblePerson { get; set; } = null!;

    public int? MaxParticipants { get; set; }

    [JsonIgnore]
    [NotMapped]
    public ICollection<User> Users { get; set; } = new List<User>();

    [JsonIgnore]
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();

    [JsonIgnore]
    public ICollection<EventSelectedType> SelectedTypes { get; set; } = new List<EventSelectedType>();

    [Required]
    [StringLength(7)]
    public string Color { get; set; } = null!;

    [JsonPropertyName("categories")]
    public ICollection<string> CategoryNames =>
        EventCategories?.Select(c => c.Category?.Name ?? string.Empty).ToList()
        ?? new List<string>();

    [JsonPropertyName("eventTypes")]
    public ICollection<EventTypeKind> EventTypeKinds =>
        SelectedTypes?.Select(t => t.TypeKind).ToList() ?? new List<EventTypeKind>();

    [JsonIgnore]
    public ICollection<EventRole> EventRoles { get; set; } = new List<EventRole>();

    [JsonIgnore]
    public ICollection<EventPhoto> Photos { get; set; } = new List<EventPhoto>();

    [JsonIgnore]
    public ICollection<EventAttachment> Attachments { get; set; } = new List<EventAttachment>();
    
    [JsonIgnore]
    public ICollection<EventNote> Notes { get; set; } = new List<EventNote>();

    [NotMapped]
    [JsonPropertyName("previewPhotos")]
    public List<string> PreviewPhotos => Photos?.Take(4).Select(p => p.FilePath).ToList() ?? new();

    /// <summary>Устаревшее строковое поле статуса; оставлено для обратной совместимости сериализации до полного перехода клиентов.</summary>
    [NotMapped]
    [JsonPropertyName("status")]
    public string? LegacyStatusDisplay => LifecycleState.ToString();
}
