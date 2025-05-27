using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

public class Roles
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    
    public Guid EventId { get; set; }
    [NotMapped]
    [JsonIgnore]
    public ICollection<Event> Events { get; set; }

    [JsonIgnore]
    public ICollection<EventRole> EventRoles { get; set; }
}