using SEM.Domain.Models;

namespace Domain.DTO;

public sealed class GlobalSearchResponse
{
    public string Query { get; set; } = string.Empty;

    public List<GlobalSearchUserDto> Users { get; set; } = new();
    public List<GlobalSearchEventDto> Events { get; set; } = new();
    public List<GlobalSearchEventDto> ArchivedEvents { get; set; } = new();
    public List<RecentSearchQueryDto> RecentQueries { get; set; } = new();
}

public sealed class GlobalSearchUserDto
{
    public Guid Id { get; set; }
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    public string? Profession { get; set; }
    public string? City { get; set; }
    public string? AvatarUrl { get; set; }
}

public sealed class GlobalSearchEventDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Auditorium { get; set; }
    public VenueFormat VenueFormat { get; set; }
    public EventLifecycleState LifecycleState { get; set; }
    public string Color { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}

public sealed class RecentSearchQueryDto
{
    public string Query { get; set; } = string.Empty;
    public DateTime LastUsedAt { get; set; }
    public int UseCount { get; set; }
}