using Shared.Uploads;

namespace Worker.Uploader.Tests;

public class FileChunkStorageTests
{
    [Fact]
    public async Task SaveChunks_ShouldReportComplete_WhenAllChunksPresent()
    {
        var uploadId = $"test-{Guid.NewGuid():N}";
        var storage = new FileChunkStorage();

        try
        {
            var firstChunkMetadata = new ChunkMetadata
            {
                UploadId = uploadId,
                FileName = "demo.bin",
                ChunkNumber = 0,
                TotalChunks = 2
            };
            await storage.SaveChunkAsync(firstChunkMetadata, new MemoryStream([1, 2, 3]), CancellationToken.None);

            var secondChunkMetadata = new ChunkMetadata
            {
                UploadId = uploadId,
                FileName = "demo.bin",
                ChunkNumber = 1,
                TotalChunks = 2
            };
            await storage.SaveChunkAsync(secondChunkMetadata, new MemoryStream([4, 5]), CancellationToken.None);

            var status = await storage.GetUploadStatusAsync(uploadId, CancellationToken.None);

            Assert.True(status.IsComplete);
            Assert.Equal(2, status.ReceivedChunks);
            Assert.Equal(2, status.TotalChunks);
        }
        finally
        {
            CleanupUpload(uploadId);
        }
    }

    [Fact]
    public async Task GetUploadStatus_ShouldNotReportComplete_WhenChunkIsMissing()
    {
        var uploadId = $"test-{Guid.NewGuid():N}";
        var storage = new FileChunkStorage();

        try
        {
            var firstChunkMetadata = new ChunkMetadata
            {
                UploadId = uploadId,
                FileName = "demo.bin",
                ChunkNumber = 0,
                TotalChunks = 3
            };
            await storage.SaveChunkAsync(firstChunkMetadata, new MemoryStream([1]), CancellationToken.None);

            var lastChunkMetadata = new ChunkMetadata
            {
                UploadId = uploadId,
                FileName = "demo.bin",
                ChunkNumber = 2,
                TotalChunks = 3
            };
            await storage.SaveChunkAsync(lastChunkMetadata, new MemoryStream([3]), CancellationToken.None);

            var status = await storage.GetUploadStatusAsync(uploadId, CancellationToken.None);

            Assert.False(status.IsComplete);
            Assert.Equal(2, status.ReceivedChunks);
        }
        finally
        {
            CleanupUpload(uploadId);
        }
    }

    [Fact]
    public void GetUploadDirectory_ShouldRejectPathTraversal()
    {
        Assert.Throws<ArgumentException>(() => FileChunkStorage.GetUploadDirectory("..\\outside"));
    }

    [Fact]
    public async Task TryCreateEnqueuedMarker_ShouldBeIdempotent()
    {
        var uploadId = $"test-{Guid.NewGuid():N}";
        var storage = new FileChunkStorage();

        try
        {
            var first = await storage.TryCreateEnqueuedMarkerAsync(uploadId, CancellationToken.None);
            var second = await storage.TryCreateEnqueuedMarkerAsync(uploadId, CancellationToken.None);

            Assert.True(first);
            Assert.False(second);
        }
        finally
        {
            CleanupUpload(uploadId);
        }
    }

    [Fact]
    public async Task AssembleFile_ShouldConcatenateChunks()
    {
        var uploadId = $"test-{Guid.NewGuid():N}";
        var storage = new FileChunkStorage();
        string? assembledPath = null;

        try
        {
            var firstChunkMetadata = new ChunkMetadata
            {
                UploadId = uploadId,
                FileName = "payload.bin",
                ChunkNumber = 0,
                TotalChunks = 2
            };
            await storage.SaveChunkAsync(firstChunkMetadata, new MemoryStream([10, 11]), CancellationToken.None);

            var secondChunkMetadata = new ChunkMetadata
            {
                UploadId = uploadId,
                FileName = "payload.bin",
                ChunkNumber = 1,
                TotalChunks = 2
            };
            await storage.SaveChunkAsync(secondChunkMetadata, new MemoryStream([12, 13]), CancellationToken.None);

            assembledPath = await storage.AssembleFileAsync(uploadId, CancellationToken.None);

            var bytes = await File.ReadAllBytesAsync(assembledPath);
            Assert.Equal([10, 11, 12, 13], bytes);
        }
        finally
        {
            CleanupUpload(uploadId);
            if (!string.IsNullOrWhiteSpace(assembledPath) && File.Exists(assembledPath))
            {
                File.Delete(assembledPath);
            }
        }
    }

    private static void CleanupUpload(string uploadId)
    {
        var uploadDirectory = FileChunkStorage.GetUploadDirectory(uploadId);
        if (Directory.Exists(uploadDirectory))
        {
            Directory.Delete(uploadDirectory, recursive: true);
        }
    }
}
