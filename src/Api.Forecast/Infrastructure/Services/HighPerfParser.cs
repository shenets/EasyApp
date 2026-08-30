using Api.Forecast.Application.Services;

namespace Api.Forecast.Infrastructure.Services;

public sealed class HighPerfParser : IHighPerfParser
{
    public int Sum(ReadOnlySpan<byte> data)
    {
        int sum = 0;

        foreach (var b in data)
            sum += b;

        return sum;
    }
}
