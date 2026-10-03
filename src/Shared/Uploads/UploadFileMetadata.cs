namespace Shared.Uploads;

public sealed class UploadFileMetadata
{
    public string FileName { get; init; } = string.Empty;

    public int TotalChunks { get; init; }
}
