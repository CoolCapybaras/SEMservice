using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

public class Invites
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid InviterId { get; set; }
    public Guid InvitedUserId { get; set; }
    public InviteStatus Status { get; set; } = InviteStatus.Pending;
    
    public DateTime InvitedAt { get; set; }

    [JsonIgnore]
    public Event Event { get; set; }
    [JsonIgnore]
    public User Inviter { get; set; }
    [JsonIgnore]
    public User InvitedUser { get; set; }
}

public enum InviteStatus
{
    Pending,
    Accepted,
    Declined
}