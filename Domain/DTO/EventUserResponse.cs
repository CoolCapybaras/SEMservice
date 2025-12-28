namespace Domain.DTO;

public class EventUserResponse
{
    public Guid id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string Profession { get; set; }
    public string PhoneNumber { get; set; }
    public string Telegram { get; set; }
    public string City { get; set; }
    public string AvatarUrl { get; set; }
    public string Role { get; set; }
    public bool IsContact {get; set;}
}