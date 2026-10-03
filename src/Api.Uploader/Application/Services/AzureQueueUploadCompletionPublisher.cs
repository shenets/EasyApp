using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using Shared.Uploads;

namespace Api.Uploader.Application.Services;

public sealed class AzureQueueUploadCompletionPublisher : IUploadCompletionPublisher
{
    private readonly QueueClient _queueClient;

    public AzureQueueUploadCompletionPublisher(IOptions<UploadStorageOptions> options)
    {
        _queueClient = StorageClientFactory.CreateQueueClient(options.Value);
    }

    public async Task PublishAsync(UploadCompletedMessage message, CancellationToken ct)
    {
        await _queueClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var payload = JsonSerializer.Serialize(message);
        await _queueClient.SendMessageAsync(payload, cancellationToken: ct);
    }
}
