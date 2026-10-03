namespace Shared.Uploads;

public sealed record UploadCompletedMessage(
    string UploadId,
    string FileName,
    int TotalChunks,
    DateTimeOffset CompletedAtUtc);
