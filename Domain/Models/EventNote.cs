using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

public class EventNote
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
    [MaxLength(10000)]
    public string Text { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}