using System.ComponentModel.DataAnnotations;

namespace Dal.UserProfiles.Models;

public class UserProfile
{
    [Key]
    public Guid Id { get; set; }
    
    public string? LastName { get; set; }
    
    public string? FirstName { get; set; }
    
    public string? MiddleName { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public string? Telegram { get; set; }
    
    public string? City { get; set; }
    
    public string? EducationalInstitution { get; set; }
    
    public int? CourseNumber { get; set; }
} 