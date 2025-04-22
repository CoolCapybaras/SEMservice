using System.ComponentModel.DataAnnotations;

namespace SEM.Domain.Models;

public class Event
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    public string Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; }

    [Required]
    [StringLength(50)]
    public string Format { get; set; }

    [Required]
    [StringLength(50)]
    public string EventType { get; set; }

    public string ResponsiblePerson { get; set; }

    public int? MaxParticipants { get; set; }

    public bool IsActive => DateTime.UtcNow < EndDate;
    
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
}