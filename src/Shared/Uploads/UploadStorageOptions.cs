namespace Shared.Uploads;

public sealed class UploadStorageOptions
{
    public const string SectionName = "UploadStorage";

    public string? ConnectionString { get; init; }

    public string AccountName { get; init; } = string.Empty;

    public string BlobContainerName { get; init; } = "uploads";

    public string QueueName { get; init; } = "upload-completed";

    public string ValidationQueueName { get; init; } = "upload-validation";

    public string? SasToken { get; init; }

    public string? AccountKey { get; init; }
}
