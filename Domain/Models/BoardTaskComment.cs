using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

public class BoardTaskComment
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TaskId { get; set; }
    [JsonIgnore]
    public BoardTask Task { get; set; } = null!;

    [Required]
    public Guid AuthorId { get; set; }
    [JsonIgnore]
    public User Author { get; set; } = null!;

    [Required]
    [MaxLength(4000)]
    public string Text { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}