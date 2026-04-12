namespace SEM.Domain.Models;

/// <summary>Отображаемые имени четырёх фиксированных ролей участника мероприятия.</summary>
public static class EventRoleTemplates
{
    public const string Organizer = "Организатор";
    public const string Editor = "Редактор";
    public const string Assistant = "Помощник";
    public const string Observer = "Наблюдатель";

    public static string NameFor(ParticipantRoleKind kind) =>
        kind switch
        {
            ParticipantRoleKind.Organizer => Organizer,
            ParticipantRoleKind.Editor => Editor,
            ParticipantRoleKind.Assistant => Assistant,
            ParticipantRoleKind.Observer => Observer,
            _ => Observer
        };

    public static ParticipantRoleKind KindFromRoleName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ParticipantRoleKind.Observer;
        return name.Trim() switch
        {
            Organizer => ParticipantRoleKind.Organizer,
            Editor => ParticipantRoleKind.Editor,
            Assistant => ParticipantRoleKind.Assistant,
            Observer => ParticipantRoleKind.Observer,
            "Участник" => ParticipantRoleKind.Observer,
            _ => ParticipantRoleKind.Observer
        };
    }
}