using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Forecast.Domain.Services;
using Api.Forecast.Infrastructure;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.EntityFrameworkCore;
using Microsoft.VSDiagnostics;
using ForecastEntity = Api.Forecast.Domain.Entities.Forecast;

namespace EasyApp.Benchmarks.Api.Forecast;

[MemoryDiagnoser]
[CPUUsageDiagnoser]
public class ForecastServiceBenchmark
{
    private readonly Consumer _consumer = new();

    private ForecastDbContext _dbContext = null!;

    private ForecastService _service = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ForecastDbContext>()
            .UseSqlite("Data Source=forecast.db")
            .Options;

        _dbContext = new ForecastDbContext(options);
        _ = _dbContext.Database.EnsureDeleted();
        _ = _dbContext.Database.EnsureCreated();

        _service = new ForecastService(_dbContext);
    }

    [GlobalCleanup]
    public void Cleanup() => _dbContext.Dispose();

    [Benchmark]
    public void GetForecast() => _consumer.Consume(_service.GetForecast(10));

    [Benchmark]
    public void GetForecastYield() => _consumer.Consume(_service.GetForecastYield(10));

    [Benchmark]
    public async Task ForecastAsync()
    {
        await foreach (var item in _service.GetForecastAsync(10))
        {
        }
    }

    [Benchmark]
    public Task<List<ForecastEntity>> ForecastSpanAsync() => _service.GetForecastSpanAsync(10);

    [Benchmark]
    public void ForecastArrayPool()
    {
        var arr = _service.GetArrayPooled(10);
        ArrayPool<ForecastEntity>.Shared.Return(arr);
    }
}
