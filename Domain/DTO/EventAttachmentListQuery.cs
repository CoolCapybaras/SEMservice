namespace Domain.DTO;

/// <summary>Параметры списка вложений: поиск, фильтры, сортировка.</summary>
public class EventAttachmentListQuery
{
    /// <summary>Поиск по названию, имени файла или URL.</summary>
    public string? Q { get; set; }

    /// <summary>Типы через запятую: File, Link. Пусто — все.</summary>
    public string? Kinds { get; set; }

    /// <summary>Идентификаторы авторов через запятую.</summary>
    public string? AuthorIds { get; set; }

    /// <summary>Расширения файлов: docx, pdf или .docx (только для Kind=File).</summary>
    public string? Extensions { get; set; }

    /// <summary>Ключи площадок для ссылок (как в facets), через запятую: figma,google-docs.</summary>
    public string? LinkSites { get; set; }

    /// <summary>Newest | Oldest | TitleAsc | AuthorAsc</summary>
    public string Sort { get; set; } = "Newest";
}