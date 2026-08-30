using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using Api.Forecast.Application.Services;
using Api.Forecast.Infrastructure.Memory;

namespace Api.Forecast.Application.Endpoints;

public static class HighPerfEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/sum/single-read", async (HttpContext ctx, IHighPerfParser parser) =>
        {
            using var pooled = new PooledBuffer(1024);

            int read = await ctx.Request.Body.ReadAsync(pooled.Memory);

            long sum = parser.Sum(pooled.Span[..read]);
            await WriteSumResponseAsync(ctx, "single-read", sum, read, read > 0 ? 1 : 0);
        });

        app.MapPost("/api/sum/chunked", async (HttpContext ctx, IHighPerfParser parser) =>
        {
            using var pooled = new PooledBuffer(1024);

            long sum = 0;
            long bytesRead = 0;
            int chunks = 0;

            while (true)
            {
                int read = await ctx.Request.Body.ReadAsync(pooled.Memory);
                if (read == 0)
                {
                    break;
                }

                chunks++;
                bytesRead += read;
                sum += parser.Sum(pooled.Span[..read]);
            }

            await WriteSumResponseAsync(ctx, "chunked-1024", sum, bytesRead, chunks);
        });

        app.MapPost("/api/sum/chunked/{bufferSize:int}", async (HttpContext ctx, IHighPerfParser parser, int bufferSize) =>
        {
            bufferSize = Math.Clamp(bufferSize, 64, 65536);
            using var pooled = new PooledBuffer(bufferSize);

            long sum = 0;
            long bytesRead = 0;
            int chunks = 0;

            while (true)
            {
                int read = await ctx.Request.Body.ReadAsync(pooled.Memory);
                if (read == 0)
                {
                    break;
                }

                chunks++;
                bytesRead += read;
                sum += parser.Sum(pooled.Span[..read]);
            }

            await WriteSumResponseAsync(ctx, $"chunked-{bufferSize}", sum, bytesRead, chunks);
        });

        app.MapPost("/api/sum/body-reader", async (HttpContext ctx, IHighPerfParser parser) =>
        {
            PipeReader reader = ctx.Request.BodyReader;

            long sum = 0;
            long bytesRead = 0;
            int chunks = 0;

            while (true)
            {
                ReadResult result = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                if (!buffer.IsEmpty)
                {
                    chunks++;
                    bytesRead += buffer.Length;

                    foreach (ReadOnlyMemory<byte> segment in buffer)
                    {
                        sum += parser.Sum(segment.Span);
                    }
                }

                reader.AdvanceTo(buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            await WriteSumResponseAsync(ctx, "body-reader", sum, bytesRead, chunks);
        });
    }

    private static async Task WriteSumResponseAsync(HttpContext ctx, string mode, long sum, long bytesRead, int chunks)
    {
        ctx.Response.ContentType = "application/json";

        var output = new ArrayBufferWriter<byte>(128);
        using var writer = new Utf8JsonWriter(output, new JsonWriterOptions
        {
            SkipValidation = true
        });

        writer.WriteStartObject();
        writer.WriteString("mode", mode);
        writer.WriteNumber("sum", sum);
        writer.WriteNumber("bytesRead", bytesRead);
        writer.WriteNumber("chunks", chunks);
        writer.WriteEndObject();

        await writer.FlushAsync();
        await ctx.Response.Body.WriteAsync(output.WrittenMemory);
    }
}

