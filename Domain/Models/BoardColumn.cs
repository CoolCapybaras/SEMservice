using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

public class BoardColumn
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    public int Order { get; set; }
    
    [Required]
    public Guid EventId { get; set; }

    [JsonIgnore]
    public Event Event { get; set; }

    [JsonIgnore]
    public ICollection<BoardTask> Tasks { get; set; } = new List<BoardTask>();
}