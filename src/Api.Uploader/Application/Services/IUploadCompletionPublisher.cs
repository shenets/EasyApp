using Shared.Uploads;

namespace Api.Uploader.Application.Services;

public interface IUploadCompletionPublisher
{
    Task PublishAsync(UploadCompletedMessage message, CancellationToken ct);
}
