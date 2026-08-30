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

//var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
//    .WithReference(gateway)
//    .WaitFor(server);

//server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
