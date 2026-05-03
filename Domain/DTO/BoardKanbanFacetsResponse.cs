namespace Domain.DTO;

/// <summary>Динамические значения для фильтра «Исполнитель» по задачам доски мероприятия.</summary>
public class BoardKanbanFacetsResponse
{
    public List<BoardKanbanAssigneeFacet> Assignees { get; set; } = new();
}

public class BoardKanbanAssigneeFacet
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}