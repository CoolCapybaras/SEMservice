namespace Domain.DTO;

public sealed class AssigneeCandidateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Profession { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty; // "Editor" | "Assistant"
}