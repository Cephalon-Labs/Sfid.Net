using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SfidNet.EntityFramework;
using WebApplication1.Data;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("WeatherForecasts")
    ?? throw new InvalidOperationException("Connection string 'WeatherForecasts' is missing.");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSnowfake(builder.Configuration, builder.Environment);
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<WeatherForecastDbContext>(options =>
    options.UseSqlite(connectionString)
           .UseSfidEntityFramework());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("WebApplication1 API");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WeatherForecastDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();
