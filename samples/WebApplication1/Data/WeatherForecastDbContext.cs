using Microsoft.EntityFrameworkCore;
using SfidNet.EntityFramework;

namespace WebApplication1.Data;

public sealed class WeatherForecastDbContext(DbContextOptions<WeatherForecastDbContext> options) : DbContext(options)
{
    public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.AssignSnowfakeKeys();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        this.AssignSnowfakeKeys();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeatherForecast>(entity =>
        {
            entity.HasKey(forecast => forecast.Id);
            entity.Property(forecast => forecast.Id).HasSnowfakeKey();
            entity.Property(forecast => forecast.Summary).HasMaxLength(128);
        });
    }
}
