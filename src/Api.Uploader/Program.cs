using Api.Uploader.Application.Controllers;
using Api.Uploader.Application.Services;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Shared.Uploads;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddOptions<UploadStorageOptions>()
    .Bind(builder.Configuration.GetSection(UploadStorageOptions.SectionName));
builder.Services.AddSingleton<BlobContainerClient>(provider =>
{
    var options = provider.GetRequiredService<IOptions<UploadStorageOptions>>().Value;
    return StorageClientFactory.CreateBlobContainerClient(options);
});
builder.Services.AddSingleton<IChunkStorage, BlobChunkStorage>();
builder.Services.AddSingleton<IUploadCompletionPublisher, AzureQueueUploadCompletionPublisher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapGet("/", () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", uploader = "running" }));

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
