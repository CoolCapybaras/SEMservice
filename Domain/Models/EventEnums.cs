using System.Text.Json.Serialization;

namespace SEM.Domain.Models;

/// <summary>Статус мероприятия (фиксированный набор из макета).</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventLifecycleState
{
    Draft,
    Published,
    Completed,
    Cancelled,
    Archived
}

/// <summary>Формат проведения: очно / онлайн / гибрид.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VenueFormat
{
    InPerson,
    Online,
    Hybrid
}

/// <summary>Роль участника в рамках мероприятия.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ParticipantRoleKind
{
    Organizer,
    Editor,
    Assistant,
    Observer
}

/// <summary>Тип мероприятия (множественный выбор).</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventTypeKind
{
    Hackathon,
    Lecture,
    PP,
    SpecialCourse,
    Practice,
    CareerEvent
}