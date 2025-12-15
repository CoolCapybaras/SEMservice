namespace Domain.DTO;

public class BoardTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid CreatorId { get; set; }
    public DateTime? DueDate { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string Status { get; set; } = null!;
}