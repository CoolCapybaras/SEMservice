using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string PasswordHash { get; set; }
    
    public string? LastName { get; set; }

    [Required] 
    public string? FirstName { get; set; } = "User";
    
    public string? MiddleName { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public string? Telegram { get; set; }
    
    public string? City { get; set; }

    public string? ResetToken { get; set; }
    
    public string? AvatarUrl { get; set; }

    public UserPrivilege UserPrivilege { get; set; } = UserPrivilege.COMMON;
    
    [JsonIgnore]
    public ICollection<EventRole> EventRole { get; set; }
} 

public enum UserPrivilege
{
    COMMON,
    ORGANIZER,
    ADMIN
}