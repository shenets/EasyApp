using Worker.Uploader.Services;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using Shared.Uploads;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddOptions<UploadStorageOptions>()
    .Bind(builder.Configuration.GetSection(UploadStorageOptions.SectionName));
builder.Services.AddSingleton<IChunkStorage, BlobChunkStorage>();
builder.Services.AddSingleton<IPoisonQueueSink, AzurePoisonQueueSink>();
builder.Services.AddSingleton<IValidationRequestPublisher, AzureValidationRequestPublisher>();
builder.Services.AddSingleton<IUploadCompletionProcessor, UploadCompletionProcessor>();
builder.Services.AddSingleton<QueueClient>(provider =>
{
    var options = provider.GetRequiredService<IOptions<UploadStorageOptions>>().Value;
    return StorageClientFactory.CreateQueueClient(options);
});
builder.Services.AddSingleton<BlobContainerClient>(provider =>
{
    var options = provider.GetRequiredService<IOptions<UploadStorageOptions>>().Value;
    return StorageClientFactory.CreateBlobContainerClient(options);
});
builder.Services.AddHostedService<Worker.Uploader.Worker>();

var host = builder.Build();
host.Run();
