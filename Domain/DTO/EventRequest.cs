namespace Domain.DTO;

public class EventRequest
{
    public string Name { get; set; }

    public string Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Location { get; set; }

    public string Format { get; set; }

    public string EventType { get; set; }

    public Guid ResponsiblePersonId { get; set; }

    public int? MaxParticipants { get; set; }

    public List<string> Categories { get; set; }
    
    public List<string> Roles { get; set; }
}