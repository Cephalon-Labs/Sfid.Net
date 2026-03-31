using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SfidNet;
using WebApplication1.Contracts;
using WebApplication1.Data;

namespace WebApplication1.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly WeatherForecastDbContext _dbContext;
    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, WeatherForecastDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet(Name = "GetWeatherForecasts")]
    public async Task<ActionResult<IReadOnlyList<WeatherForecastResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var forecasts = await _dbContext.WeatherForecasts
            .OrderBy(forecast => forecast.Date)
            .ThenBy(forecast => forecast.Id)
            .ToListAsync(cancellationToken);

        return forecasts.Select(MapResponse).ToArray();
    }

    [HttpGet("{id}", Name = "GetWeatherForecastById")]
    public async Task<ActionResult<WeatherForecastResponse>> GetByIdAsync(Sfid id, CancellationToken cancellationToken)
    {
        var forecast = await _dbContext.WeatherForecasts
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (forecast is null)
        {
            _logger.LogInformation("Weather forecast {WeatherForecastId} was not found.", id);
            return NotFound();
        }

        return MapResponse(forecast);
    }

    [HttpPost]
    public async Task<ActionResult<WeatherForecastResponse>> CreateAsync(
        CreateWeatherForecastRequest request,
        CancellationToken cancellationToken)
    {
        var forecast = new WeatherForecast
        {
            Date = request.Date,
            TemperatureC = request.TemperatureC,
            Summary = request.Summary,
        };

        _dbContext.WeatherForecasts.Add(forecast);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created weather forecast {WeatherForecastId}.", forecast.Id);

        return CreatedAtRoute("GetWeatherForecastById", new { id = forecast.Id }, MapResponse(forecast));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<WeatherForecastResponse>> UpdateAsync(
        Sfid id,
        UpdateWeatherForecastRequest request,
        CancellationToken cancellationToken)
    {
        var forecast = await _dbContext.WeatherForecasts
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (forecast is null)
        {
            _logger.LogInformation("Weather forecast {WeatherForecastId} was not found for update.", id);
            return NotFound();
        }

        forecast.Date = request.Date;
        forecast.TemperatureC = request.TemperatureC;
        forecast.Summary = request.Summary;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated weather forecast {WeatherForecastId}.", forecast.Id);

        return Ok(MapResponse(forecast));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Sfid id, CancellationToken cancellationToken)
    {
        var forecast = await _dbContext.WeatherForecasts
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (forecast is null)
        {
            _logger.LogInformation("Weather forecast {WeatherForecastId} was not found for deletion.", id);
            return NotFound();
        }

        _dbContext.WeatherForecasts.Remove(forecast);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted weather forecast {WeatherForecastId}.", id);

        return NoContent();
    }

    private static WeatherForecastResponse MapResponse(WeatherForecast forecast)
        => new(
            forecast.Id,
            forecast.Date,
            forecast.TemperatureC,
            forecast.TemperatureF,
            forecast.Summary);
}
