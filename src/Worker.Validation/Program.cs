using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using Shared.Uploads;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddOptions<UploadStorageOptions>()
    .Bind(builder.Configuration.GetSection(UploadStorageOptions.SectionName));
builder.Services.AddSingleton<QueueClient>(provider =>
{
    UploadStorageOptions options = provider.GetRequiredService<IOptions<UploadStorageOptions>>().Value;
    UploadStorageOptions validationOptions = new()
    {
        AccountName = options.AccountName,
        AccountKey = options.AccountKey,
        BlobContainerName = options.BlobContainerName,
        ConnectionString = options.ConnectionString,
        QueueName = options.ValidationQueueName,
        SasToken = options.SasToken
    };

    return StorageClientFactory.CreateQueueClient(validationOptions);
});
builder.Services.AddSingleton<BlobContainerClient>(provider =>
{
    UploadStorageOptions options = provider.GetRequiredService<IOptions<UploadStorageOptions>>().Value;
    return StorageClientFactory.CreateBlobContainerClient(options);
});
builder.Services.AddHostedService<global::Worker.Validation.Worker>();

await builder.Build().RunAsync();
