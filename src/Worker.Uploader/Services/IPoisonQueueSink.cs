using Azure.Storage.Queues.Models;

namespace Worker.Uploader.Services;

public interface IPoisonQueueSink
{
    Task StoreAsync(QueueMessage message, Exception exception, CancellationToken ct);
}
