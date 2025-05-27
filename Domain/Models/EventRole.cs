using Microsoft.EntityFrameworkCore.Metadata;

namespace SEM.Domain.Models;

public class EventRole
{
    
    public Guid EventId { get; set; }
    public Event Event { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; }

    public Guid RoleId { get; set; }
    public Roles Role { get; set; }

    public bool IsContact { get; set; } = false;
}