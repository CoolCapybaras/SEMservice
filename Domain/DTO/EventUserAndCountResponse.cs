namespace Domain.DTO;

public class EventUserAndCountResponse
{
    public List<EventUserResponse> Users { get; set; } = new();
    public int TotalCount { get; set; }
}