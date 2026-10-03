using System.Buffers;
using ClosedXML.Excel;
using Api.Forecast.Infrastructure;
using ForecastEntity = Api.Forecast.Domain.Entities.Forecast;
using Microsoft.EntityFrameworkCore;

namespace Api.Forecast.Domain.Services;

public class ForecastService : IForecastDomainService
{
    private const int ImportBatchSize = 256;

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

    public async Task<int> ImportFromExcelAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            return 0;
        }

        var rows = worksheet.RowsUsed().Skip(1);
        var pool = ArrayPool<ForecastEntity>.Shared;
        var buffer = pool.Rent(ImportBatchSize);
        var count = 0;
        var inserted = 0;

        try
        {
            foreach (var row in rows)
            {
                if (!TryMapForecast(row, out var forecast))
                {
                    continue;
                }

                buffer[count++] = forecast;

                if (count == ImportBatchSize)
                {
                    inserted += await SaveBatchAsync(buffer, count, cancellationToken);
                    count = 0;
                }
            }

            if (count > 0)
            {
                inserted += await SaveBatchAsync(buffer, count, cancellationToken);
            }
        }
        finally
        {
            Array.Clear(buffer, 0, count);
            pool.Return(buffer);
        }

        return inserted;
    }

    private async Task<int> SaveBatchAsync(ForecastEntity[] buffer, int count, CancellationToken cancellationToken)
    {
        for (var i = 0; i < count; i++)
        {
            Context.Forecasts.Add(buffer[i]);
            buffer[i] = null!;
        }

        var saved = await Context.SaveChangesAsync(cancellationToken);
        Context.ChangeTracker.Clear();
        return saved;
    }

    private static bool TryMapForecast(IXLRow row, out ForecastEntity forecast)
    {
        forecast = null!;

        DateOnly date;
        var dateCell = row.Cell(1);
        if (dateCell.TryGetValue<DateTime>(out var dateTime))
        {
            date = DateOnly.FromDateTime(dateTime);
        }
        else
        {
            var dateSpan = dateCell.GetString().AsSpan().Trim();
            if (!DateOnly.TryParse(dateSpan, out date))
            {
                return false;
            }
        }

        var tempSpan = row.Cell(2).GetString().AsSpan().Trim();
        if (!int.TryParse(tempSpan, out var temperatureC))
        {
            return false;
        }

        var summary = row.Cell(3).GetString().Trim();
        if (summary.Length == 0)
        {
            return false;
        }

        if (summary.Length > 200)
        {
            summary = summary[..200];
        }

        forecast = new ForecastEntity
        {
            Date = date,
            TemperatureC = temperatureC,
            Summary = summary
        };

        return true;
    }
}
