using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

public class EventChatAttachment
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }
    [JsonIgnore]
    public EventChatMessage? Message { get; set; }
    
    public string FilePath { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}