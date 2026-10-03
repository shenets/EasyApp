namespace Api.Forecast.Application.Models;

public sealed class ChunkMetadata
{
    public string UploadId { get; init; } = default!;
    public string FileName { get; init; } = default!;
    public int ChunkNumber { get; init; }
    public int TotalChunks { get; init; }
}

