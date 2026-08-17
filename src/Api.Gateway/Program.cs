var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.AddControllers();
//builder.Services.AddSwaggerGen();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

//builder.Services.AddCors();
//builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();

    app.MapGet("/", () => Results.Redirect("/health"))
        .ExcludeFromDescription();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", gateway = "running" }));

//app.UseHttpsRedirection();
//app.UseAuthorization();

//app.MapControllers();
app.MapReverseProxy();

app.Run();
