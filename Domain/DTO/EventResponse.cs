using System.Text.Json.Serialization;
using SEM.Domain.Models;

namespace Domain.DTO;

public class EventResponse
{
    public Guid Id { get; set; }
    
    public string Name { get; set; }

    public string Description { get; set; }
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public string Location { get; set; }
    
    public string Format { get; set; }
    
    public string EventType { get; set; }

    public Guid ResponsiblePersonId { get; set; }

    public int? MaxParticipants { get; set; }
    
    public string Color { get; set; }
    
    public List<string> Categories { get; set; }
    
    public List<string> PreviewPhotos { get; set; }
    
    public string? status { get; set; }
    
    public List<UserResponse> Participants { get; set; }
    
    public int ParticipantsCount { get; set; }
}