using Api.Forecast.Infrastructure;
using ForecastEntity = Api.Forecast.Domain.Entities.Forecast;
using Microsoft.EntityFrameworkCore;
using System.Buffers;

namespace Api.Forecast.Domain.Services;

public class ForecastService : IForecastDomainService
{
    private readonly ForecastDbContext _dbContext;

    public ForecastService(ForecastDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private ForecastDbContext Context => _dbContext;

    public IEnumerable<ForecastEntity> GetForecast(int count)
    {
        return Context.Forecasts
            .AsNoTracking()
            .Take(count);
    }

    public IEnumerable<ForecastEntity> GetForecastYield(int count)
    {
        foreach (var item in Context.Forecasts.AsNoTracking().Take(count))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<ForecastEntity> GetForecastAsync(int count)
    {
        var query = Context.Forecasts
            .AsNoTracking()
            .Take(count)
            .AsAsyncEnumerable();

        await foreach (var item in query)
        {
            yield return item;
        }
    }

    public async Task<List<ForecastEntity>> GetForecastSpanAsync(int count)
    {
        return await Context.Forecasts
            .AsNoTracking()
            .Take(count)
            .ToListAsync();
    }

    public ForecastEntity[] GetArrayPooled(int count)
    {
        var pool = ArrayPool<ForecastEntity>.Shared;
        var arr = pool.Rent(count);

        var index = 0;
        foreach (var item in GetForecastYield(count))
        {
            arr[index++] = item;
        }

        return arr;
    }

    public List<ForecastEntity> GetAll()
    {
        return Context.Forecasts
            .AsNoTracking()
            .ToList();
    }

    public Task<List<ForecastEntity>> GetAllAsync()
    {
        return Context.Forecasts
            .AsNoTracking()
            .ToListAsync();
    }

    public IAsyncEnumerable<ForecastEntity> StreamAllAsync()
    {
        return Context.Forecasts
            .AsNoTracking()
            .AsAsyncEnumerable();
    }
}
