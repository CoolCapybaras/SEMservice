using Microsoft.AspNetCore.Http;

namespace Domain.DTO;

public class EventUpdateRequest
{

    public string Name { get; set; }

    public string Description { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string Location { get; set; }

    public string Format { get; set; }

    public string EventType { get; set; }

    public int? MaxParticipants { get; set; }
    
    public IFormFile? Avatar { get; set; }
}