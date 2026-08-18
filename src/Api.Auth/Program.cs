using Api.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<TokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/health"))
        .ExcludeFromDescription();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", auth = "running" }));

app.MapControllers();

// Run the application
app.Run();
