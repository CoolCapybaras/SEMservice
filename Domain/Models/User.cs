using System.ComponentModel.DataAnnotations;

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
    
    public string? EducationalInstitution { get; set; }
    
    public int? CourseNumber { get; set; }

    public string? ResetToken { get; set; }
} 