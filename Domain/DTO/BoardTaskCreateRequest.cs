namespace Domain.DTO;

public class BoardTaskCreateRequest
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? AssignedUserId { get; set; }
    public DateTime? DueDate { get; set; }
}