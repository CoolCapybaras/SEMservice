namespace Domain.DTO;

public class BoardTaskHistoryResponse
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; }
}