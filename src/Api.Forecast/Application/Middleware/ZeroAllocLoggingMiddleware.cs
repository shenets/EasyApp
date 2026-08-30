namespace Api.Forecast.Application.Middleware;

public sealed class ZeroAllocLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ZeroAllocLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext ctx)
    {
        var path = (ctx.Request.Path.Value ?? string.Empty).AsSpan();

        Span<char> buffer = stackalloc char[128];
        var written = Math.Min(path.Length, buffer.Length);
        path[..written].CopyTo(buffer);

        Console.WriteLine($"REQ: {new string(buffer[..written])}");

        await _next(ctx);
    }
}

