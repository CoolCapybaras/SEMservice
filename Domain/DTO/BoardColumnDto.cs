namespace Domain.DTO;

public class BoardColumnDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Order { get; set; }
    public List<BoardTaskDto> Tasks { get; set; } = new();
}