using Microsoft.EntityFrameworkCore;
using ForecastEntity = Api.Forecast.Domain.Entities.Forecast;

namespace Api.Forecast.Infrastructure;

public class ForecastDbContext : DbContext
{
    public DbSet<ForecastEntity> Forecasts => Set<ForecastEntity>();

    public ForecastDbContext(DbContextOptions<ForecastDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.LazyLoadingEnabled = false;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ForecastEntity>(entity =>
        {
            entity.ToTable("Forecasts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Date).IsRequired();
            entity.Property(x => x.TemperatureC).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(200).IsRequired();

            entity.HasData(
                new ForecastEntity
                {
                    Id = 1,
                    Date = new DateOnly(2026, 1, 1),
                    TemperatureC = 3,
                    Summary = "Cold"
                },
                new ForecastEntity
                {
                    Id = 2,
                    Date = new DateOnly(2026, 1, 2),
                    TemperatureC = 8,
                    Summary = "Cool"
                },
                new ForecastEntity
                {
                    Id = 3,
                    Date = new DateOnly(2026, 1, 3),
                    TemperatureC = 17,
                    Summary = "Mild"
                });
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder
                .UseSqlite("Data Source=forecast.db")
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }
    }

}
