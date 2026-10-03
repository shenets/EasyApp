var builder = DistributedApplication.CreateBuilder(args);

var forecastPath = builder.Configuration["Sqlite:ForecastPath"] ?? "forecast.db";
var forecastDirectory = Path.GetDirectoryName(forecastPath);
var forecastFileName = Path.GetFileName(forecastPath);

var forecast = string.IsNullOrWhiteSpace(forecastDirectory)
    ? builder.AddSqlite("forecast", databaseFileName: forecastFileName)
    : builder.AddSqlite("forecast", forecastDirectory, forecastFileName)
    .WithSqliteWeb();

var server = builder.AddProject<Projects.EasyApp_Server>("Server")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var gateway = builder.AddProject<Projects.Api_Gateway>("ApiGateway")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var auth = builder.AddProject<Projects.Api_Auth>("ApiAuth")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var weatherForecast = builder.AddProject<Projects.Api_Forecast>("ApiForecast")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(forecast);

var uploadStorageAccountName = builder.Configuration["UploadStorage:AccountName"] ?? string.Empty;
var uploadStorageAccountKey = builder.Configuration["UploadStorage:AccountKey"] ?? string.Empty;
var uploadStorageBlobContainerName = builder.Configuration["UploadStorage:BlobContainerName"] ?? "uploads";
var uploadStorageConnectionString = builder.Configuration["UploadStorage:ConnectionString"] ?? string.Empty;
var uploadStorageQueueName = builder.Configuration["UploadStorage:QueueName"] ?? "upload-completed";
var uploadStorageValidationQueueName = builder.Configuration["UploadStorage:ValidationQueueName"] ?? "upload-validation";
var uploadStorageSasToken = builder.Configuration["UploadStorage:SasToken"] ?? string.Empty;

var uploaderApi = builder.AddProject<Projects.Api_Uploader>("ApiUploader")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithEnvironment("UploadStorage__AccountName", uploadStorageAccountName)
    .WithEnvironment("UploadStorage__AccountKey", uploadStorageAccountKey)
    .WithEnvironment("UploadStorage__BlobContainerName", uploadStorageBlobContainerName)
    .WithEnvironment("UploadStorage__ConnectionString", uploadStorageConnectionString)
    .WithEnvironment("UploadStorage__QueueName", uploadStorageQueueName)
    .WithEnvironment("UploadStorage__ValidationQueueName", uploadStorageValidationQueueName)
    .WithEnvironment("UploadStorage__SasToken", uploadStorageSasToken);

builder.AddProject<Projects.Worker_Uploader>("UploaderWorker")
    .WithReference(uploaderApi)
    .WithEnvironment("UploadStorage__AccountName", uploadStorageAccountName)
    .WithEnvironment("UploadStorage__AccountKey", uploadStorageAccountKey)
    .WithEnvironment("UploadStorage__BlobContainerName", uploadStorageBlobContainerName)
    .WithEnvironment("UploadStorage__ConnectionString", uploadStorageConnectionString)
    .WithEnvironment("UploadStorage__QueueName", uploadStorageQueueName)
    .WithEnvironment("UploadStorage__ValidationQueueName", uploadStorageValidationQueueName)
    .WithEnvironment("UploadStorage__SasToken", uploadStorageSasToken);

builder.AddProject<Projects.Worker_Validation>("ValidationWorker")
    .WithEnvironment("UploadStorage__AccountName", uploadStorageAccountName)
    .WithEnvironment("UploadStorage__AccountKey", uploadStorageAccountKey)
    .WithEnvironment("UploadStorage__BlobContainerName", uploadStorageBlobContainerName)
    .WithEnvironment("UploadStorage__ConnectionString", uploadStorageConnectionString)
    .WithEnvironment("UploadStorage__QueueName", uploadStorageQueueName)
    .WithEnvironment("UploadStorage__ValidationQueueName", uploadStorageValidationQueueName)
    .WithEnvironment("UploadStorage__SasToken", uploadStorageSasToken);

//var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
//    .WithReference(gateway)
//    .WaitFor(server);

//server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
