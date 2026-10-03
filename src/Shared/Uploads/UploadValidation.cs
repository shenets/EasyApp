using System.Text.RegularExpressions;

namespace Shared.Uploads;

public static partial class UploadValidation
{
    public const int MaxChunkSizeBytes = 16 * 1024 * 1024;
    public const long MaxUploadFileSizeBytes = 1024L * 1024 * 1024;
    public const int MaxChunkCount = 1_000_000;

    public static bool IsValidUploadId(string? uploadId)
    {
        return !string.IsNullOrWhiteSpace(uploadId)
            && uploadId.Length <= 100
            && UploadIdRegex().IsMatch(uploadId);
    }

    public static bool IsValidFileName(string? fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName)
            && fileName.Length <= 255
            && fileName.IndexOfAny(['/', '\\', '|']) < 0
            && fileName.All(character => !char.IsControl(character));
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex UploadIdRegex();
}
