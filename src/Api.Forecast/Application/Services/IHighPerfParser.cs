namespace Api.Forecast.Application.Services
{
    public interface IHighPerfParser
    {
        int Sum(ReadOnlySpan<byte> data);
    }
}
