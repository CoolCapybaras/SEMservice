using SEM.Domain.Models;

namespace Domain.DTO;

/// <summary>Справочная запись о фиксированной роли (без таблицы Roles в БД).</summary>
public class EventFixedRoleInfoDto
{
    public int Id { get; set; }
    public ParticipantRoleKind ParticipantRole { get; set; }
    public string Name { get; set; } = null!;
}