using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

public class EventPost
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; }
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public Event Event { get; set; }
    [JsonIgnore]
    public User Author { get; set; }
}