namespace Domain.DTO;

public class BoardDto
{
    public Guid EventId { get; set; }
    public List<BoardColumnDto> Columns { get; set; } = new();
}