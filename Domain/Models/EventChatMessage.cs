using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

public class EventChatMessage
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }
    [JsonIgnore]
    public Event? Event { get; set; }

    public Guid UserId { get; set; }
    [JsonIgnore]
    public User? User { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}