namespace Domain.DTO;

public class EventNoteResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}