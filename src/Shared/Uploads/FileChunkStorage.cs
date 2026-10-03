namespace Shared.Uploads;

public sealed class FileChunkStorage : IChunkStorage
{
    private const string EnqueuedMarkerName = ".enqueued";
    private readonly string _chunksRoot = Path.Combine("Uploads", "Chunks");

    public FileChunkStorage()
    {
        Directory.CreateDirectory(_chunksRoot);
    }

    public async Task SaveChunkAsync(ChunkMetadata meta, Stream chunkStream, CancellationToken ct)
    {
        ValidateMetadata(meta);

        var uploadDir = GetUploadDirectory(meta.UploadId);
        Directory.CreateDirectory(uploadDir);

        var chunkPath = Path.Combine(uploadDir, $"{meta.ChunkNumber:D6}.chunk");
        var metaPath = Path.Combine(uploadDir, "meta.txt");
        if (File.Exists(metaPath))
        {
            var existingMetadata = GetUploadMetadata(uploadDir);
            ValidateMetadataMatch(existingMetadata, meta);
        }
        else
        {
            await File.WriteAllTextAsync(metaPath, $"{meta.FileName}|{meta.TotalChunks}", ct);
        }

        await using var output = new FileStream(chunkPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await chunkStream.CopyToAsync(output, ct);
    }

    public Task DeleteUploadArtifactsAsync(string uploadId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var uploadDir = GetUploadDirectory(uploadId);
        if (Directory.Exists(uploadDir))
        {
            Directory.Delete(uploadDir, recursive: true);
        }

        return Task.CompletedTask;
    }

    public Task<UploadStatus> GetUploadStatusAsync(string uploadId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var uploadDir = GetUploadDirectory(uploadId);
        if (!Directory.Exists(uploadDir))
        {
            return Task.FromResult(new UploadStatus(uploadId, 0, 0, false));
        }

        var metadata = GetUploadMetadata(uploadDir);
        var chunks = Directory.GetFiles(uploadDir, "*.chunk")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => int.TryParse(name, out var number)
                && number >= 0
                && number < metadata.TotalChunks)
            .Select(name => int.Parse(name!))
            .Distinct()
            .ToArray();
        var isComplete = chunks.Length == metadata.TotalChunks
            && Enumerable.Range(0, metadata.TotalChunks).All(chunks.Contains);
        return Task.FromResult(new UploadStatus(uploadId, chunks.Length, metadata.TotalChunks, isComplete));
    }

    public Task DeleteEnqueuedMarkerAsync(string uploadId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var markerPath = Path.Combine(GetUploadDirectory(uploadId), EnqueuedMarkerName);
        if (File.Exists(markerPath))
        {
            File.Delete(markerPath);
        }

        return Task.CompletedTask;
    }

    public Task<UploadFileMetadata> GetUploadMetadataAsync(string uploadId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var uploadDir = GetUploadDirectory(uploadId);
        if (!Directory.Exists(uploadDir))
        {
            throw new InvalidOperationException($"Upload '{uploadId}' not found.");
        }

        return Task.FromResult(GetUploadMetadata(uploadDir));
    }

    public async Task<string> AssembleFileAsync(string uploadId, CancellationToken ct)
    {
        var uploadDir = GetUploadDirectory(uploadId);
        var metadata = GetUploadMetadata(uploadDir);

        var finalDir = Path.Combine("Uploads", "Final");
        Directory.CreateDirectory(finalDir);

        var safeFileName = Path.GetFileName(metadata.FileName);
        var finalPath = Path.Combine(finalDir, $"{uploadId}-{safeFileName}");

        await using var output = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None);

        for (var i = 0; i < metadata.TotalChunks; i++)
        {
            var chunkPath = Path.Combine(uploadDir, $"{i:D6}.chunk");
            await using var input = new FileStream(chunkPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await input.CopyToAsync(output, ct);
        }

        return finalPath;
    }

    public Task<bool> TryCreateEnqueuedMarkerAsync(string uploadId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var uploadDir = GetUploadDirectory(uploadId);
        Directory.CreateDirectory(uploadDir);

        var markerPath = Path.Combine(uploadDir, EnqueuedMarkerName);

        try
        {
            using var marker = new FileStream(markerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            marker.WriteByte(1);
            return Task.FromResult(true);
        }
        catch (IOException)
        {
            return Task.FromResult(false);
        }
    }

    public static string GetUploadDirectory(string uploadId)
    {
        ValidateUploadId(uploadId);
        return Path.Combine("Uploads", "Chunks", uploadId);
    }

    public static string GetEnqueuedMarkerPath(string uploadId)
    {
        return Path.Combine(GetUploadDirectory(uploadId), EnqueuedMarkerName);
    }

    private static UploadFileMetadata GetUploadMetadata(string uploadDir)
    {
        var metaPath = Path.Combine(uploadDir, "meta.txt");
        if (!File.Exists(metaPath))
        {
            throw new InvalidOperationException("Upload metadata not found.");
        }

        var parts = File.ReadAllText(metaPath).Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var totalChunks))
        {
            throw new InvalidOperationException("Upload metadata has invalid format.");
        }

        var metadata = new UploadFileMetadata
        {
            FileName = parts[0],
            TotalChunks = totalChunks
        };
        if (!UploadValidation.IsValidFileName(metadata.FileName) || metadata.TotalChunks <= 0 || metadata.TotalChunks > UploadValidation.MaxChunkCount)
        {
            throw new InvalidOperationException("Upload metadata has invalid values.");
        }

        return metadata;
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

    private static void ValidateMetadataMatch(UploadFileMetadata existingMetadata, ChunkMetadata meta)
    {
        if (!string.Equals(existingMetadata.FileName, meta.FileName, StringComparison.Ordinal) || existingMetadata.TotalChunks != meta.TotalChunks)
        {
            throw new InvalidOperationException("Chunk metadata mismatch for existing upload.");
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
