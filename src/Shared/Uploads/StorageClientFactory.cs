using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;

namespace Shared.Uploads;

public static class StorageClientFactory
{
    public static QueueClient CreateQueueClient(UploadStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new QueueClient(options.ConnectionString, options.QueueName);
        }

        var queueUri = new Uri($"https://{options.AccountName}.queue.core.windows.net/{options.QueueName}");

        if (!string.IsNullOrWhiteSpace(options.SasToken))
        {
            return new QueueClient(queueUri, new AzureSasCredential(options.SasToken.TrimStart('?')));
        }

        if (!string.IsNullOrWhiteSpace(options.AccountKey))
        {
            return new QueueClient(queueUri, new StorageSharedKeyCredential(options.AccountName, options.AccountKey));
        }

        throw new InvalidOperationException("UploadStorage requires ConnectionString or (SasToken/AccountKey with AccountName).");
    }

    public static BlobContainerClient CreateBlobContainerClient(UploadStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new BlobContainerClient(options.ConnectionString, options.BlobContainerName);
        }

        var blobUri = new Uri($"https://{options.AccountName}.blob.core.windows.net/{options.BlobContainerName}");

        if (!string.IsNullOrWhiteSpace(options.SasToken))
        {
            return new BlobContainerClient(blobUri, new AzureSasCredential(options.SasToken.TrimStart('?')));
        }

        if (!string.IsNullOrWhiteSpace(options.AccountKey))
        {
            return new BlobContainerClient(blobUri, new StorageSharedKeyCredential(options.AccountName, options.AccountKey));
        }

        throw new InvalidOperationException("UploadStorage requires ConnectionString or (SasToken/AccountKey with AccountName).");
    }
}
