using SfidNet;

namespace WebApplication1.Contracts;

public sealed record WeatherForecastResponse(
    Sfid Id,
    DateOnly Date,
    int TemperatureC,
    int TemperatureF,
    string? Summary);
