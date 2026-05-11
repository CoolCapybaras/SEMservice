using SEM.Domain.Models;

namespace Domain.DTO;

public class BoardTasksResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventAvatarUrl { get; set; } = string.Empty;
    public Guid ColumnId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid CreatorId { get; set; }
    public DateTime? DueDate { get; set; }
    public BoardTaskPriority Priority { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}