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
    
    public ICollection<EventChatAttachment> Attachments { get; set; } = new List<EventChatAttachment>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public Guid? ReplyToMessageId { get; set; }
    [JsonIgnore]
    public EventChatMessage? ReplyToMessage { get; set; }
}