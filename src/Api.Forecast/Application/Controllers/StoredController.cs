using Api.Forecast.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using System.Buffers;
using ForecastEntity = Api.Forecast.Domain.Entities.Forecast;

namespace Api.Forecast.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoredController : ControllerBase
{
    private readonly Lazy<ForecastService> _forecastService;

    public StoredController(Lazy<ForecastService> forecastService)
    {
        _forecastService = forecastService;
    }

    private ForecastService ForecastService => _forecastService.Value;

    [HttpGet("forecast")]
    public IEnumerable<ForecastEntity> GetForecast()
    {
        return ForecastService.GetForecast(5);
    }

    [HttpGet("forecast-yield")]
    public IEnumerable<ForecastEntity> GetForecastYield()
    {
        return ForecastService.GetForecastYield(10);
    }

    [HttpGet("forecast-async")]
    public IAsyncEnumerable<ForecastEntity> GetForecastAsync()
    {
        return ForecastService.GetForecastAsync(10);
    }

    [HttpGet("span")]
    public async Task<IEnumerable<ForecastEntity>> GetSpan()
    {
        return await ForecastService.GetForecastSpanAsync(10);
    }

    [HttpGet("array-pool")]
    public IEnumerable<ForecastEntity> GetArrayPool()
    {
        var arr = ForecastService.GetArrayPooled(10);

        try
        {
            var result = new List<ForecastEntity>(10);
            for (var i = 0; i < 10; i++)
            {
                result.Add(arr[i]);
            }

            return result;
        }
        finally
        {
            ArrayPool<ForecastEntity>.Shared.Return(arr, clearArray: true);
        }
    }
}
