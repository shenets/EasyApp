using Shared.Uploads;

namespace Worker.Uploader.Services;

public interface IValidationRequestPublisher
{
    Task PublishAsync(UploadCompletedMessage message, CancellationToken ct);
}