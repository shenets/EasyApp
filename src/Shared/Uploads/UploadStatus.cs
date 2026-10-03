namespace Shared.Uploads;

public readonly record struct UploadStatus(
    string UploadId,
    int ReceivedChunks,
    int TotalChunks,
    bool IsComplete);
