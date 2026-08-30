using System.Buffers;
using Api.WeatherForecast.Models;
using Microsoft.Data.Sqlite;

namespace Api.WeatherForecast.Domain.Services;

public class ForecastService
{
    private readonly SqliteConnection _connection;

    public ForecastService(SqliteConnection connection)
    {
        _connection = connection;
    }

    private void EnsureOpenConnection()
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            _connection.Open();
        }
    }

    private static Forecast MapForecast(SqliteDataReader reader)
    {
        return new Forecast
        {
            Date = DateOnly.Parse(reader.GetString(0)),
            TemperatureC = reader.GetInt32(1),
            Summary = reader.IsDBNull(2) ? null : reader.GetString(2)
        };
    }

    public IEnumerable<Forecast> GetForecast()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Date, TemperatureC, Summary FROM Forecasts LIMIT 5";

        EnsureOpenConnection();
        using var reader = cmd.ExecuteReader();

        var result = new List<Forecast>();
        while (reader.Read())
        {
            result.Add(MapForecast(reader));
        }

        return result;
    }

    public IEnumerable<Forecast> GetPrivate(int count)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Date, TemperatureC, Summary FROM Forecasts LIMIT $count";
        cmd.Parameters.AddWithValue("$count", count);

        EnsureOpenConnection();
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            yield return MapForecast(reader);
        }
    }

    public async IAsyncEnumerable<Forecast> GetForecastAsync(int count)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Date, TemperatureC, Summary FROM Forecasts LIMIT $count";
        cmd.Parameters.AddWithValue("$count", count);

        EnsureOpenConnection();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            yield return MapForecast(reader);
        }
    }

    public Forecast[] GetArrayPooled(int count)
    {
        var pool = ArrayPool<Forecast>.Shared;
        var arr = pool.Rent(count);

        var index = 0;
        foreach (var forecast in GetPrivate(count))
        {
            arr[index++] = forecast;
        }

        return arr;
    }
}