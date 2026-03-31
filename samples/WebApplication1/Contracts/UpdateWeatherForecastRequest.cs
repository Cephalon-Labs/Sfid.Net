using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Contracts;

public sealed class UpdateWeatherForecastRequest
{
    public DateOnly Date { get; set; }

    [Range(-100, 100)]
    public int TemperatureC { get; set; }

    [StringLength(128)]
    public string? Summary { get; set; }
}
