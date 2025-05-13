using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

public class EventPhoto
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EventId { get; set; }

    [Required]
    public string FilePath { get; set; }

    [JsonIgnore]
    public Event Event { get; set; }
}