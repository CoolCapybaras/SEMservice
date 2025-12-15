namespace Domain.DTO;

public class RolesRequest
{
    public Guid EventId { get; set; }
    public int Count { get; set; }
    public int Offset { get; set; }
}