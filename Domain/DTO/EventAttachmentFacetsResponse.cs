namespace Domain.DTO;

public class EventAttachmentFacetsResponse
{
    public List<AttachmentExtensionFacet> FileExtensions { get; set; } = new();
    public List<AttachmentLinkSiteFacet> LinkSites { get; set; } = new();
    public List<AttachmentAuthorFacet> Authors { get; set; } = new();
}

public class AttachmentExtensionFacet
{
    /// <summary>Нормализованное расширение с точкой, например .docx</summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>Подпись для UI (Word, Excel, Pdf…)</summary>
    public string Label { get; set; } = string.Empty;
}

public class AttachmentLinkSiteFacet
{
    public string SiteKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class AttachmentAuthorFacet
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}