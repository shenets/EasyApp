using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Shared.Uploads;

namespace Worker.Uploader.Services;

public sealed class UploadCompletionProcessor : IUploadCompletionProcessor
{
    private readonly IChunkStorage _chunkStorage;
    private readonly BlobContainerClient _blobContainerClient;
    private readonly ILogger<UploadCompletionProcessor> _logger;
    private readonly IValidationRequestPublisher _validationRequestPublisher;

    public UploadCompletionProcessor(
        IChunkStorage chunkStorage,
        BlobContainerClient blobContainerClient,
        ILogger<UploadCompletionProcessor> logger,
        IValidationRequestPublisher validationRequestPublisher)
    {
        _chunkStorage = chunkStorage;
        _blobContainerClient = blobContainerClient;
        _logger = logger;
        _validationRequestPublisher = validationRequestPublisher;
    }

    public async Task ProcessAsync(UploadCompletedMessage message, CancellationToken ct)
    {
        var status = await _chunkStorage.GetUploadStatusAsync(message.UploadId, ct);
        if (!status.IsComplete)
        {
            throw new InvalidOperationException($"Upload '{message.UploadId}' is not complete yet.");
        }

        var metadata = await _chunkStorage.GetUploadMetadataAsync(message.UploadId, ct);
        if (!string.Equals(metadata.FileName, message.FileName, StringComparison.Ordinal) || metadata.TotalChunks != message.TotalChunks)
        {
            throw new InvalidOperationException($"Upload '{message.UploadId}' metadata does not match queue payload.");
        }

        var assembledFilePath = await _chunkStorage.AssembleFileAsync(message.UploadId, ct);
        _logger.LogInformation("Assembled upload {UploadId} at {Path}", message.UploadId, assembledFilePath);

        await _blobContainerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobName = $"uploads/{message.UploadId}/{Path.GetFileName(metadata.FileName)}";
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        await using (var fileStream = new FileStream(assembledFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            await blobClient.UploadAsync(
                fileStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = "application/octet-stream"
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["uploadId"] = message.UploadId,    
                        ["totalChunks"] = metadata.TotalChunks.ToString()
                    }
                },
                ct);
        }

        await _validationRequestPublisher.PublishAsync(message, ct);

        await _chunkStorage.DeleteUploadArtifactsAsync(message.UploadId, ct);

        if (File.Exists(assembledFilePath))
        {
            File.Delete(assembledFilePath);
        }

        _logger.LogInformation("Uploaded upload {UploadId} to blob {BlobName}", message.UploadId, blobName);
    }
}
