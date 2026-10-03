using System.Text.Json;
using Worker.Uploader.Services;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Shared.Uploads;

namespace Worker.Uploader;

public class Worker : BackgroundService
{
    private const int MaxDequeueAttempts = 5;
    private readonly ILogger<Worker> _logger;
    private readonly IPoisonQueueSink _poisonQueueSink;
    private readonly IUploadCompletionProcessor _processor;
    private readonly QueueClient _queueClient;

    public Worker(
        ILogger<Worker> logger,
        QueueClient queueClient,
        IUploadCompletionProcessor processor,
        IPoisonQueueSink poisonQueueSink)
    {
        _logger = logger;
        _queueClient = queueClient;
        _processor = processor;
        _poisonQueueSink = poisonQueueSink;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            QueueMessage[] messages;
            try
            {
                var result = await _queueClient.ReceiveMessagesAsync(maxMessages: 1, visibilityTimeout: TimeSpan.FromMinutes(2), cancellationToken: stoppingToken);
                messages = result.Value;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogWarning(ex, "Queue receive failed, backing off.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            if (messages.Length == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                continue;
            }

            var message = messages[0];
            _logger.LogInformation("Dequeued message {MessageId} with dequeue count {DequeueCount}", message.MessageId, message.DequeueCount);

            try
            {
                var payload = JsonSerializer.Deserialize<UploadCompletedMessage>(message.MessageText)
                              ?? throw new InvalidOperationException("Queue message payload is empty.");

                await _processor.ProcessAsync(payload, stoppingToken);
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
            }
            catch (Exception ex) when (ex is RequestFailedException or IOException or InvalidOperationException)
            {
                if (message.DequeueCount >= MaxDequeueAttempts)
                {
                    await _poisonQueueSink.StoreAsync(message, ex, stoppingToken);
                    await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                    _logger.LogError(ex, "Moved message {MessageId} to poison queue after {Attempts} attempts.", message.MessageId, message.DequeueCount);
                    continue;
                }

                var nextDelay = TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, Math.Min(message.DequeueCount, 8))));
                _logger.LogWarning(ex, "Upload processing failed for message {MessageId}, retry in {Delay}s.", message.MessageId, nextDelay.TotalSeconds);

                await _queueClient.UpdateMessageAsync(
                    message.MessageId,
                    message.PopReceipt,
                    message.MessageText,
                    visibilityTimeout: nextDelay,
                    cancellationToken: stoppingToken);
            }
        }
    }
}
