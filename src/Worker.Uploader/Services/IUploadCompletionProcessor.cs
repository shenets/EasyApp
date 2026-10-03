using Shared.Uploads;

namespace Worker.Uploader.Services;

public interface IUploadCompletionProcessor
{
    Task ProcessAsync(UploadCompletedMessage message, CancellationToken ct);
}
