namespace Logic.Users.Models;

public class UserLogic
{
    
    public Guid Id { get; set; }
    
    public string Email { get; set; }
    
    public string PasswordHash { get; set; }
    
    public string Username { get; set; } = "User"; // Значение по умолчанию

    public string? ResetToken { get; set; }
}