using ForecastEntity = Api.Forecast.Domain.Entities.Forecast;

namespace Api.Forecast.Domain.Services;

public interface IForecastDomainService
{
    IEnumerable<ForecastEntity> GetForecast(int count);
    IEnumerable<ForecastEntity> GetForecastYield(int count);
    IAsyncEnumerable<ForecastEntity> GetForecastAsync(int count);
    Task<List<ForecastEntity>> GetForecastSpanAsync(int count);
    ForecastEntity[] GetArrayPooled(int count);
    List<ForecastEntity> GetAll();
    Task<List<ForecastEntity>> GetAllAsync();
    IAsyncEnumerable<ForecastEntity> StreamAllAsync();
}