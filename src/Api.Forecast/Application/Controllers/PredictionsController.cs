using Api.Forecast.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using ForecastEntity = Api.Forecast.Domain.Entities.Forecast;

namespace Api.Forecast.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PredictionsController : ControllerBase
{
    private readonly Lazy<PredictionService> _predictionService;

    public PredictionsController(Lazy<PredictionService> predictionService)
    {
        _predictionService = predictionService;
    }

    private PredictionService PredictionService => _predictionService.Value;

    [HttpGet("forecast")]
    public IEnumerable<ForecastEntity> Get()
    {
        return PredictionService.GetForecast(5);
    }

    [HttpGet("forecast-async")]
    public IEnumerable<ForecastEntity> GetPrivate()
    {
        return PredictionService.GetForecastYield(10);
    }
}
