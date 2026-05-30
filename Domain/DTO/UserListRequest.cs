namespace Domain.DTO;

public class UserListRequest
{
    public string? Q { get; set; }
    public int Offset { get; set; }
    public int Count { get; set; } = 20;
}