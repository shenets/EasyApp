using Microsoft.AspNetCore.Http;

namespace Api.Uploader.Application.Controllers;

public sealed class UploadFileFormRequest
{
    public IFormFile File { get; init; } = default!;

    public string? UploadId { get; init; }

    public int? ChunkSizeBytes { get; init; }
}
