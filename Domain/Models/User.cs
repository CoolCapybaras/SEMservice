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
    
    public string? Profession { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public string? Telegram { get; set; }
    
    public string? City { get; set; }

    public string? ResetToken { get; set; }
    
    public string? AvatarUrl { get; set; }
    
    public UiTheme Theme { get; set; } = UiTheme.Light;
    
    public NotificationChannel NotificationChannel { get; set; } = NotificationChannel.None;
    
    public bool NotifyTaskAssigned { get; set; } = true;
    public bool NotifyTaskDeadline { get; set; } = true;
    public bool NotifyEventStart { get; set; } = true;
    public bool NotifyEventCancelled { get; set; } = true;

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

public enum UiTheme
{
    Light,
    Dark
}

public enum NotificationChannel
{
    None,
    Telegram,
    Vk,
    Email
}