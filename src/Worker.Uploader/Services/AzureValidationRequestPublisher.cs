using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using Shared.Uploads;

namespace Worker.Uploader.Services;

public sealed class AzureValidationRequestPublisher : IValidationRequestPublisher
{
    private readonly QueueClient _queueClient;

    public AzureValidationRequestPublisher(IOptions<UploadStorageOptions> options)
    {
        UploadStorageOptions validationOptions = new()
        {
            AccountName = options.Value.AccountName,
            AccountKey = options.Value.AccountKey,
            BlobContainerName = options.Value.BlobContainerName,
            ConnectionString = options.Value.ConnectionString,
            QueueName = options.Value.ValidationQueueName,
            SasToken = options.Value.SasToken
        };

        _queueClient = StorageClientFactory.CreateQueueClient(validationOptions);
    }

    public async Task PublishAsync(UploadCompletedMessage message, CancellationToken ct)
    {
        await _queueClient.CreateIfNotExistsAsync(cancellationToken: ct);
        await _queueClient.SendMessageAsync(JsonSerializer.Serialize(message), cancellationToken: ct);
    }
}