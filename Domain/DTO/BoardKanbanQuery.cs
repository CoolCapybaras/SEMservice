namespace Domain.DTO;

/// <summary>Фильтрация и сортировка задач на канбан-доске (внутри колонок).</summary>
public class BoardKanbanQuery
{
    /// <summary>Поиск по названию и описанию.</summary>
    public string? Q { get; set; }

    /// <summary>Бакеты дедлайна через запятую: Overdue, Today, Tomorrow, ThisWeek (логика ИЛИ).</summary>
    public string? Deadlines { get; set; }

    /// <summary>Идентификаторы исполнителей через запятую.</summary>
    public string? AssigneeIds { get; set; }

    /// <summary>Приоритеты через запятую: Urgent, High, Medium, Low (логика ИЛИ).</summary>
    public string? Priorities { get; set; }

    /// <summary>Только задачи, назначенные на текущего пользователя.</summary>
    public bool MineOnly { get; set; }

    /// <summary>UrgentFirst | Newest | Oldest | AssigneeAsc</summary>
    public string Sort { get; set; } = "UrgentFirst";
}