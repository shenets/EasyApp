namespace Shared.Uploads;

public interface IChunkStorage
{
    Task SaveChunkAsync(ChunkMetadata meta, Stream chunkStream, CancellationToken ct);

    Task<UploadStatus> GetUploadStatusAsync(string uploadId, CancellationToken ct);

    Task<UploadFileMetadata> GetUploadMetadataAsync(string uploadId, CancellationToken ct);

    Task<string> AssembleFileAsync(string uploadId, CancellationToken ct);

    Task<bool> TryCreateEnqueuedMarkerAsync(string uploadId, CancellationToken ct);

    Task DeleteEnqueuedMarkerAsync(string uploadId, CancellationToken ct);

    Task DeleteUploadArtifactsAsync(string uploadId, CancellationToken ct);
}
