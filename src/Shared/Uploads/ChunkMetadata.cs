namespace Shared.Uploads;

public sealed class ChunkMetadata
{
    public string UploadId { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public int ChunkNumber { get; init; }

    public int TotalChunks { get; init; }
}
