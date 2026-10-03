using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Options;
using Shared;
using Shared.FileReader;
using Shared.Uploads;

namespace Worker.Validation;

public sealed class Worker : BackgroundService
{
    private const int MaxDequeueAttempts = 5;
    private readonly BlobContainerClient _blobContainerClient;
    private readonly ILogger<Worker> _logger;
    private readonly QueueClient _poisonQueueClient;
    private readonly QueueClient _queueClient;

    public Worker(
        BlobContainerClient blobContainerClient,
        ILogger<Worker> logger,
        IOptions<UploadStorageOptions> options,
        QueueClient queueClient)
    {
        _blobContainerClient = blobContainerClient;
        _logger = logger;
        _queueClient = queueClient;

        UploadStorageOptions poisonOptions = new()
        {
            AccountName = options.Value.AccountName,
            AccountKey = options.Value.AccountKey,
            BlobContainerName = options.Value.BlobContainerName,
            ConnectionString = options.Value.ConnectionString,
            QueueName = $"{options.Value.ValidationQueueName}-poison",
            SasToken = options.Value.SasToken
        };
        _poisonQueueClient = StorageClientFactory.CreateQueueClient(poisonOptions);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            QueueMessage[] messages;
            try
            {
                Azure.Response<QueueMessage[]> response = await _queueClient.ReceiveMessagesAsync(
                    maxMessages: 1,
                    visibilityTimeout: TimeSpan.FromMinutes(2),
                    cancellationToken: stoppingToken);
                messages = response.Value;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogWarning(ex, "Validation queue receive failed, backing off.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            if (messages.Length == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                continue;
            }

            QueueMessage message = messages[0];
            try
            {
                UploadCompletedMessage payload = JsonSerializer.Deserialize<UploadCompletedMessage>(message.MessageText)
                    ?? throw new InvalidOperationException("Validation message payload is empty.");

                await ValidateAsync(payload, stoppingToken);
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
            }
            catch (Exception ex) when (ex is RequestFailedException or IOException or InvalidOperationException or JsonException)
            {
                if (message.DequeueCount >= MaxDequeueAttempts)
                {
                    await MoveToPoisonQueueAsync(message, ex, stoppingToken);
                    await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                    _logger.LogError(ex, "Moved validation message {MessageId} to poison queue after {Attempts} attempts.", message.MessageId, message.DequeueCount);
                    continue;
                }

                TimeSpan nextDelay = TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, Math.Min(message.DequeueCount, 8))));
                _logger.LogWarning(ex, "Validation failed for message {MessageId}, retry in {Delay}s.", message.MessageId, nextDelay.TotalSeconds);
                await _queueClient.UpdateMessageAsync(
                    message.MessageId,
                    message.PopReceipt,
                    message.MessageText,
                    visibilityTimeout: nextDelay,
                    cancellationToken: stoppingToken);
            }
        }
    }

    private async Task ValidateAsync(UploadCompletedMessage message, CancellationToken ct)
    {
        string blobName = $"uploads/{message.UploadId}/{Path.GetFileName(message.FileName)}";
        BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
        var download = await blobClient.DownloadStreamingAsync(cancellationToken: ct);

        await using Stream blobStream = download.Value.Content;
        IFileLoader<Stream> loader = new BlobStreamLoader(blobStream);
        WorksheetProcessor processor = new(Shared.WorksheetStructure.Get());
        Reader.IFileReader reader = new Reader.ClosedXMLExcelStream(processor, loader);

        int rowCount = 0;
        int invalidRowCount = 0;
        await foreach (IRow row in reader.ReadEnumerableAsync(ct))
        {
            rowCount++;
            if (row.ValidationErrors.Count > 0)
            {
                invalidRowCount++;
            }
        }

        _logger.LogInformation(
            "Validated upload {UploadId}: {Rows} rows, {InvalidRows} rows with validation errors.",
            message.UploadId,
            rowCount,
            invalidRowCount);
    }

    private async Task MoveToPoisonQueueAsync(QueueMessage message, Exception exception, CancellationToken ct)
    {
        await _poisonQueueClient.CreateIfNotExistsAsync(cancellationToken: ct);
        string payload = JsonSerializer.Serialize(new
        {
            failedAtUtc = DateTimeOffset.UtcNow,
            messageId = message.MessageId,
            dequeueCount = message.DequeueCount,
            messageText = message.MessageText,
            error = exception.Message
        });
        await _poisonQueueClient.SendMessageAsync(payload, cancellationToken: ct);
    }

    private sealed class BlobStreamLoader(Stream stream) : IFileLoader<Stream>
    {
        public Stream Load() => stream;
    }
}
