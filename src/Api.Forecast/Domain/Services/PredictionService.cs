using System.Buffers;
using ForecastEntity = Api.Forecast.Domain.Entities.Forecast;

namespace Api.Forecast.Domain.Services;

public class PredictionService : IForecastDomainService
{
    private readonly string[] _summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

    public IEnumerable<ForecastEntity> GetForecast(int count)
    {
        return Enumerable.Range(1, count).Select(GetPrediction);
    }

    public IEnumerable<ForecastEntity> GetForecastYield(int count)
    {
        for (int index = 1; index <= count; index++)
            yield return GetPrediction(index);
    }

    public async IAsyncEnumerable<ForecastEntity> GetForecastAsync(int count)
    {
        for (int index = 1; index <= count; index++)
        {
            yield return GetPrediction(index);
        }
    }

    public ForecastEntity[] GetArrayPooled(int count)
    {
        var pool = ArrayPool<ForecastEntity>.Shared;
        var arr = pool.Rent(count);

        for (int i = 0; i < count; i++)
            arr[i] = GetPrediction(i);

        return arr; // важно: вызывающий должен вернуть массив в пул
    }

    public async Task<List<ForecastEntity>> GetForecastSpanAsync(int count)
    {
        var result = new List<ForecastEntity>(count);
        await foreach (var item in GetForecastAsync(count))
        {
            result.Add(item);
        }

        return result;
    }

    public List<ForecastEntity> GetAll()
    {
        return GetForecast(5).ToList();
    }

    public Task<List<ForecastEntity>> GetAllAsync()
    {
        return GetForecastSpanAsync(5);
    }

    public IAsyncEnumerable<ForecastEntity> StreamAllAsync()
    {
        return GetForecastAsync(5);
    }


    private ForecastEntity GetPrediction(int index)
    {
        return new ForecastEntity
        {
            Id = index,
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = _summaries[Random.Shared.Next(_summaries.Length)]
        };
    }
}
