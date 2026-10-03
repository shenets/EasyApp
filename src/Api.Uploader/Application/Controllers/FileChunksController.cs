using System.Buffers;
using Api.Uploader.Application.Services;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.FileReader;
using Shared.Uploads;

namespace Api.Uploader.Application.Controllers;

[ApiController]
[Route("files")]
public class FileChunksController : ControllerBase
{
    private const int DefaultChunkSizeBytes = 4 * 1024 * 1024;
    private readonly IUploadCompletionPublisher _completionPublisher;
    private readonly BlobContainerClient _blobContainerClient;
    private readonly ILogger<FileChunksController> _logger;
    private readonly IChunkStorage _storage;

    public FileChunksController(
        IChunkStorage storage,
        IUploadCompletionPublisher completionPublisher,
        BlobContainerClient blobContainerClient,
        ILogger<FileChunksController> logger)
    {
        _completionPublisher = completionPublisher;
        _blobContainerClient = blobContainerClient;
        _logger = logger;
        _storage = storage;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadValidation.MaxUploadFileSizeBytes)]
    [RequestSizeLimit(UploadValidation.MaxUploadFileSizeBytes)]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileFormRequest request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("File is required.");
        }

        if (request.File.Length > UploadValidation.MaxUploadFileSizeBytes)
        {
            return BadRequest("File is too large.");
        }

        if (!UploadValidation.IsValidFileName(request.File.FileName))
        {
            return BadRequest("FileName has an invalid format.");
        }

        var uploadId = string.IsNullOrWhiteSpace(request.UploadId)
            ? Guid.NewGuid().ToString("N")
            : request.UploadId;

        if (!UploadValidation.IsValidUploadId(uploadId))
        {
            return BadRequest("UploadId has an invalid format.");
        }

        var chunkSizeBytes = request.ChunkSizeBytes.GetValueOrDefault(DefaultChunkSizeBytes);
        if (chunkSizeBytes <= 0 || chunkSizeBytes > UploadValidation.MaxChunkSizeBytes)
        {
            return BadRequest($"ChunkSizeBytes must be between 1 and {UploadValidation.MaxChunkSizeBytes}.");
        }

        var totalChunks = checked((int)((request.File.Length - 1) / chunkSizeBytes + 1));
        if (totalChunks > UploadValidation.MaxChunkCount)
        {
            return BadRequest("The file contains too many chunks.");
        }

        await using var input = request.File.OpenReadStream();

        var buffer = ArrayPool<byte>.Shared.Rent(chunkSizeBytes);

        try
        {
            var chunkNumber = 0;
            while (true)
            {
                var bytesRead = await input.ReadAsync(buffer.AsMemory(0, chunkSizeBytes), ct);
                if (bytesRead == 0)
                {
                    break;
                }

                await using var chunkStream = new MemoryStream(buffer, 0, bytesRead, writable: false, publiclyVisible: true);

                IFileLoader<Stream> loader = new ChunkStreamLoader(chunkStream);

                WorksheetProcessor processor = new(WorksheetStructure.Get());

                Reader.IFileReader reader = new Reader.ClosedXMLExcelStream(processor, loader);

                await foreach (IRow row in reader.ReadEnumerableAsync(ct))
                {
                    // ... process each row as needed
                }

                chunkStream.Position = 0;

                var chunkMetadata = new ChunkMetadata
                {
                    UploadId = uploadId,
                    FileName = request.File.FileName,
                    ChunkNumber = chunkNumber,
                    TotalChunks = totalChunks
                };

                await _storage.SaveChunkAsync(chunkMetadata, chunkStream, ct);

                chunkNumber++;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        var status = await _storage.GetUploadStatusAsync(uploadId, ct);
        await EnqueueIfCompleteAsync(uploadId, status, ct);

        return Ok(new
        {
            uploadId,
            fileName = request.File.FileName,
            status
        });
    }

    [HttpPost("{uploadId}/chunks")]
    [RequestSizeLimit(UploadValidation.MaxChunkSizeBytes)]
    public async Task<IActionResult> UploadChunk(
        string uploadId,
        [FromQuery] int chunkNumber,
        [FromQuery] int totalChunks,
        [FromQuery] string fileName,
        CancellationToken ct)
    {
        if (!UploadValidation.IsValidUploadId(uploadId))
        {
            return BadRequest("UploadId has an invalid format.");
        }

        if (chunkNumber < 0
            || totalChunks <= 0
            || chunkNumber >= totalChunks
            || totalChunks > UploadValidation.MaxChunkCount)
        {
            return BadRequest("Chunk metadata is invalid.");
        }

        if (!UploadValidation.IsValidFileName(fileName))
        {
            return BadRequest("FileName has an invalid format.");
        }

        if (Request.ContentLength is 0 or > UploadValidation.MaxChunkSizeBytes)
        {
            return BadRequest("Chunk size is invalid.");
        }

        await _storage.SaveChunkAsync(
            new ChunkMetadata
            {
                UploadId = uploadId,
                FileName = fileName,
                ChunkNumber = chunkNumber,
                TotalChunks = totalChunks
            },
            Request.Body,
            ct);

        _logger.LogInformation(
            "Chunk saved: uploadId={UploadId}, chunk={ChunkNumber}/{TotalChunks}",
            uploadId,
            chunkNumber,
            totalChunks);

        var status = await _storage.GetUploadStatusAsync(uploadId, ct);
        await EnqueueIfCompleteAsync(uploadId, status, ct);

        return Ok(status);
    }

    [HttpGet("{uploadId}/status")]
    public async Task<IActionResult> GetStatus(string uploadId, CancellationToken ct)
    {
        if (!UploadValidation.IsValidUploadId(uploadId))
        {
            return BadRequest("UploadId has an invalid format.");
        }

        var status = await _storage.GetUploadStatusAsync(uploadId, ct);
        return Ok(status);
    }

    [HttpGet("azure/blob/status")]
    public async Task<IActionResult> GetBlobConnectionStatus(CancellationToken ct)
    {
        try
        {
            var containerExists = await _blobContainerClient.ExistsAsync(ct);

            return Ok(new
            {
                connected = true,
                accountName = _blobContainerClient.AccountName,
                container = _blobContainerClient.Name,
                containerExists = containerExists.Value
            });
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Blob connection check failed.");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                connected = false,
                errorCode = ex.ErrorCode,
                message = ex.Message
            });
        }
    }

    private async Task EnqueueIfCompleteAsync(string uploadId, UploadStatus status, CancellationToken ct)
    {
        if (!status.IsComplete || !await _storage.TryCreateEnqueuedMarkerAsync(uploadId, ct))
        {
            return;
        }

        var metadata = await _storage.GetUploadMetadataAsync(uploadId, ct);
        try
        {
            await _completionPublisher.PublishAsync(
                new UploadCompletedMessage(
                    uploadId,
                    metadata.FileName,
                    metadata.TotalChunks,
                    DateTimeOffset.UtcNow),
                ct);
        }
        catch
        {
            await _storage.DeleteEnqueuedMarkerAsync(uploadId, CancellationToken.None);
            throw;
        }
    }
}

public sealed class ChunkStreamLoader : IFileLoader<Stream>
{
    private readonly Stream _stream;

    public ChunkStreamLoader(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _stream = stream;
    }

    public Stream Load() => _stream;
}
