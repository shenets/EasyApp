using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Shared.Uploads;

public sealed class BlobChunkStorage : IChunkStorage
{
    private const string ChunksPrefix = "chunks";
    private const string EnqueuedMarkerName = ".enqueued";
    private const string MetadataBlobName = "meta.json";
    private readonly BlobContainerClient _blobContainerClient;

    public BlobChunkStorage(BlobContainerClient blobContainerClient)
    {
        _blobContainerClient = blobContainerClient;
    }

    public async Task SaveChunkAsync(ChunkMetadata meta, Stream chunkStream, CancellationToken ct)
    {
        ValidateMetadata(meta);
        await _blobContainerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var chunkBlobClient = _blobContainerClient.GetBlobClient(GetChunkBlobName(meta.UploadId, meta.ChunkNumber));
        await chunkBlobClient.UploadAsync(chunkStream, overwrite: true, cancellationToken: ct);

        await EnsureMetadataAsync(meta, ct);
    }

    public async Task<UploadStatus> GetUploadStatusAsync(string uploadId, CancellationToken ct)
    {
        var metadata = await TryGetUploadMetadataAsync(uploadId, ct);
        if (metadata is null)
        {
            return new UploadStatus(uploadId, 0, 0, false);
        }

        var uploadPrefix = GetUploadPrefix(uploadId);
        var receivedChunks = new bool[metadata.TotalChunks];
        var chunkCount = 0;
        await foreach (var blobItem in _blobContainerClient.GetBlobsAsync(prefix: uploadPrefix, cancellationToken: ct))
        {
            var chunkName = blobItem.Name[uploadPrefix.Length..];
            if (chunkName.EndsWith(".chunk", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(chunkName[..^6], out var chunkNumber)
                && chunkNumber >= 0
                && chunkNumber < metadata.TotalChunks
                && !receivedChunks[chunkNumber])
            {
                receivedChunks[chunkNumber] = true;
                chunkCount++;
            }
        }

        return new UploadStatus(uploadId, chunkCount, metadata.TotalChunks, chunkCount == metadata.TotalChunks);
    }

    public async Task<UploadFileMetadata> GetUploadMetadataAsync(string uploadId, CancellationToken ct)
    {
        var metadata = await TryGetUploadMetadataAsync(uploadId, ct);
        if (metadata is null)
        {
            throw new InvalidOperationException($"Upload '{uploadId}' metadata not found.");
        }

        return metadata;
    }

    public async Task<string> AssembleFileAsync(string uploadId, CancellationToken ct)
    {
        var metadata = await GetUploadMetadataAsync(uploadId, ct);

        var finalDir = Path.Combine("Uploads", "Final");
        Directory.CreateDirectory(finalDir);

        var finalPath = Path.Combine(finalDir, $"{uploadId}-{Path.GetFileName(metadata.FileName)}");
        await using var output = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None);

        for (var i = 0; i < metadata.TotalChunks; i++)
        {
            var chunkBlobClient = _blobContainerClient.GetBlobClient(GetChunkBlobName(uploadId, i));
            var download = await chunkBlobClient.DownloadStreamingAsync(cancellationToken: ct);
            await download.Value.Content.CopyToAsync(output, ct);
        }

        return finalPath;
    }

    public async Task<bool> TryCreateEnqueuedMarkerAsync(string uploadId, CancellationToken ct)
    {
        ValidateUploadId(uploadId);
        await _blobContainerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var markerBlobClient = _blobContainerClient.GetBlobClient(GetMarkerBlobName(uploadId));
        var content = new BinaryData("1");

        try
        {
            await markerBlobClient.UploadAsync(content, new BlobUploadOptions
            {
                Conditions = new BlobRequestConditions
                {
                    IfNoneMatch = ETag.All
                }
            }, ct);

            return true;
        }
        catch (RequestFailedException ex) when (ex.Status is 409 or 412)
        {
            return false;
        }
    }

    public async Task DeleteEnqueuedMarkerAsync(string uploadId, CancellationToken ct)
    {
        ValidateUploadId(uploadId);
        await _blobContainerClient.DeleteBlobIfExistsAsync(GetMarkerBlobName(uploadId), cancellationToken: ct);
    }

    public async Task DeleteUploadArtifactsAsync(string uploadId, CancellationToken ct)
    {
        await foreach (var blobItem in _blobContainerClient.GetBlobsAsync(prefix: GetUploadPrefix(uploadId), cancellationToken: ct))
        {
            await _blobContainerClient.DeleteBlobIfExistsAsync(blobItem.Name, cancellationToken: ct);
        }
    }

    private async Task EnsureMetadataAsync(ChunkMetadata meta, CancellationToken ct)
    {
        var metadataBlobClient = _blobContainerClient.GetBlobClient(GetMetadataBlobName(meta.UploadId));
        var payload = JsonSerializer.Serialize(new UploadFileMetadata
        {
            FileName = meta.FileName,
            TotalChunks = meta.TotalChunks
        });

        try
        {
            await metadataBlobClient.UploadAsync(
                BinaryData.FromString(payload),
                new BlobUploadOptions
                {
                    Conditions = new BlobRequestConditions
                    {
                        IfNoneMatch = ETag.All
                    }
                },
                ct);
            return;
        }
        catch (RequestFailedException ex) when (ex.Status is 409 or 412)
        {
        }

        var existing = await GetUploadMetadataAsync(meta.UploadId, ct);
        if (!string.Equals(existing.FileName, meta.FileName, StringComparison.Ordinal)
            || existing.TotalChunks != meta.TotalChunks)
        {
            throw new InvalidOperationException("Chunk metadata mismatch for existing upload.");
        }
    }

    private async Task<UploadFileMetadata?> TryGetUploadMetadataAsync(string uploadId, CancellationToken ct)
    {
        var metadataBlobClient = _blobContainerClient.GetBlobClient(GetMetadataBlobName(uploadId));
        if (!await metadataBlobClient.ExistsAsync(ct))
        {
            return null;
        }

        var download = await metadataBlobClient.DownloadContentAsync(ct);
        var metadata = download.Value.Content.ToObjectFromJson<UploadFileMetadata>();
        if (metadata is null)
        {
            throw new InvalidOperationException($"Upload '{uploadId}' metadata has invalid format.");
        }

        return metadata;
    }

    private static string GetChunkBlobName(string uploadId, int chunkNumber)
    {
        ValidateUploadId(uploadId);
        return $"{GetUploadPrefix(uploadId)}{chunkNumber:D6}.chunk";
    }

    private static string GetMarkerBlobName(string uploadId)
    {
        return $"{GetUploadPrefix(uploadId)}{EnqueuedMarkerName}";
    }

    private static string GetMetadataBlobName(string uploadId)
    {
        return $"{GetUploadPrefix(uploadId)}{MetadataBlobName}";
    }

    private static string GetUploadPrefix(string uploadId)
    {
        ValidateUploadId(uploadId);
        return $"{ChunksPrefix}/{uploadId}/";
    }

    private static void ValidateMetadata(ChunkMetadata meta)
    {
        ValidateUploadId(meta.UploadId);
        if (!UploadValidation.IsValidFileName(meta.FileName)
            || meta.ChunkNumber < 0
            || meta.TotalChunks <= 0
            || meta.ChunkNumber >= meta.TotalChunks
            || meta.TotalChunks > UploadValidation.MaxChunkCount)
        {
            throw new ArgumentException("Chunk metadata is invalid.", nameof(meta));
        }
    }

    private static void ValidateUploadId(string uploadId)
    {
        if (!UploadValidation.IsValidUploadId(uploadId))
        {
            throw new ArgumentException("UploadId has an invalid format.", nameof(uploadId));
        }
    }
}
