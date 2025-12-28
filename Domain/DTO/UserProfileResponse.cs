namespace Domain.DTO;

public class UserProfileResponse
{
    public Guid Id { get; set; }
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? Profession { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Telegram { get; set; }
    public string? City { get; set; }
    
    public string UserPrivilege { get; set; }
        
    public string? AvatarUrl { get; set; }
}

