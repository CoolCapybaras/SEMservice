namespace Domain.DTO;

public class UserSerchRequest
{
    public string? FirstName { get; set; }
    public string? Profession { get; set; }
    public string? LastName { get; set; }
    public string? City { get; set; }
    public int Offset { get; set; }
    public int Count { get; set; }
}