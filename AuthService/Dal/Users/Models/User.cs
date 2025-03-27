

using System.ComponentModel.DataAnnotations;

namespace Dal.Users.Models;


public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string PasswordHash { get; set; }

    [Required]
    public string Username { get; set; } = "User";

    public string? ResetToken { get; set; }
}