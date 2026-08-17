var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.EasyApp_Server>("Server")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var gateway = builder.AddProject<Projects.Api_Gateway>("ApiGateway")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var weatherForecast = builder.AddProject<Projects.Api_WeatherForecast>("ApiForecast")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

//var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
//    .WithReference(gateway)
//    .WaitFor(server);

//server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
