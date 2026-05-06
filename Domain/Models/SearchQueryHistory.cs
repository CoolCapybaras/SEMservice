using System.ComponentModel.DataAnnotations;

namespace SEM.Domain.Models;

public sealed class SearchQueryHistory
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [StringLength(256)]
    public string Query { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string NormalizedQuery { get; set; } = string.Empty;

    public DateTime LastUsedAt { get; set; }

    public int UseCount { get; set; } = 1;
}