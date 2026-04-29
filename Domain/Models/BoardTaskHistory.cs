using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

public class BoardTaskHistory
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TaskId { get; set; }
    [JsonIgnore]
    public BoardTask Task { get; set; } = null!;

    [Required]
    public Guid ChangedByUserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = null!;

    [MaxLength(100)]
    public string? FieldName { get; set; }

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}