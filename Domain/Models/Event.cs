﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

    public Guid ResponsiblePersonId { get; set; }
    [JsonIgnore]
    public User ResponsiblePerson { get; set; }

    public int MaxParticipants { get; set; }

    [JsonIgnore]
    public ICollection<User> Users { get; set; }

    [JsonIgnore]
    public ICollection<EventCategory> EventCategories { get; set; }

    [JsonPropertyName("categories")]
    public ICollection<string> CategoryNames => EventCategories.Select(c => c.Category.Name).ToList();
}