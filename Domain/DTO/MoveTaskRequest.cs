namespace Domain.DTO;

public class MoveTaskRequest
{
    public Guid TargetColumnId { get; set; }
    public int NewOrder { get; set; }
}