namespace SEM.Domain.Models;

/// <summary>Приоритет задачи на канбан-доске (для сортировки «сначала срочные»).</summary>
public enum BoardTaskPriority
{
    Urgent = 0,
    High = 1,
    Medium = 2,
    Low = 3
}