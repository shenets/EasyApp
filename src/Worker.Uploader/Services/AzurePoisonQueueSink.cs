using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Options;
using Shared.Uploads;

namespace Worker.Uploader.Services;

public sealed class AzurePoisonQueueSink : IPoisonQueueSink
{
    private readonly QueueClient _poisonQueueClient;

    public AzurePoisonQueueSink(IOptions<UploadStorageOptions> options)
    {
        var poisonOptions = new UploadStorageOptions
        {
            AccountName = options.Value.AccountName,
            AccountKey = options.Value.AccountKey,
            SasToken = options.Value.SasToken,
            BlobContainerName = options.Value.BlobContainerName,
            QueueName = $"{options.Value.QueueName}-poison"
        };

        _poisonQueueClient = StorageClientFactory.CreateQueueClient(poisonOptions);
    }

    public async Task StoreAsync(QueueMessage message, Exception exception, CancellationToken ct)
    {
        await _poisonQueueClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var payload = JsonSerializer.Serialize(new
        {
            failedAtUtc = DateTimeOffset.UtcNow,
            messageId = message.MessageId,
            dequeueCount = message.DequeueCount,
            messageText = message.MessageText,
            error = exception.Message
        });

        await _poisonQueueClient.SendMessageAsync(payload, cancellationToken: ct);
    }
}
