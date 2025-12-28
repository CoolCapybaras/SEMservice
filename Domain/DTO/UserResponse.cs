namespace Domain.DTO;

public class UserResponse
{
    public Guid Id { get; set; }
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? Profession { get; set; }
    public string? AvatarUrl { get; set; }
    
    public bool IsContact {get; set;}
}