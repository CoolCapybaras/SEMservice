namespace Domain.DTO;

public sealed class DownloadMediaResult
{
    public byte[] Bytes { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = "file";
}