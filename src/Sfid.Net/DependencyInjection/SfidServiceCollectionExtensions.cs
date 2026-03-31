using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SfidNet;
using SfidNet.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers Snowfake runtime and generator services with the default .NET dependency injection container.
/// </summary>
public static class SfidServiceCollectionExtensions
{
    /// <summary>
    /// Bootstraps Snowfake from configuration and the current host environment.
    /// </summary>
    public static IServiceCollection AddSnowfake(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        return services.AddSnowfake(configuration, environment.ApplicationName, ResolveConfiguredInstanceId(configuration));
    }

    /// <summary>
    /// Bootstraps Snowfake from configuration and explicit application metadata.
    /// </summary>
    public static IServiceCollection AddSnowfake(
        this IServiceCollection services,
        IConfiguration configuration,
        string applicationName,
        string? instanceId = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = configuration.GetSection(SfidSettings.SectionName).Get<SfidSettings>() ?? new SfidSettings();
        var resolvedApplicationName = SfidApplicationIdentityResolver.ResolveApplicationName(applicationName);
        var resolvedInstanceId = SfidApplicationIdentityResolver.ResolveInstanceId(
            settings.InstanceId ?? instanceId,
            resolvedApplicationName);

        var nodeIdentity = SfidNodeIdentityResolver.Resolve(settings, resolvedApplicationName, resolvedInstanceId);

        SfidRuntime.Bootstrap(new SfidOptions
        {
            CustomEpoch = settings.CustomEpoch ?? SfidDefaults.TwitterEpoch,
            ClockRegressionTolerance = TimeSpan.FromMilliseconds(Math.Max(0, settings.ClockRegressionToleranceMilliseconds)),
            DatacenterId = nodeIdentity.DatacenterId,
            WorkerId = nodeIdentity.WorkerId,
            WorkerCapacity = nodeIdentity.WorkerCapacity
        });

        services.AddSingleton<ISfidGenerator>(_ => SfidRuntime.Current);
        return services;
    }

    /// <summary>
    /// Bootstraps Snowfake from code instead of configuration.
    /// </summary>
    public static IServiceCollection AddSnowfake(
        this IServiceCollection services,
        Action<SfidOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SfidOptions();
        configure(options);

        SfidRuntime.Bootstrap(options);
        services.AddSingleton<ISfidGenerator>(_ => SfidRuntime.Current);
        return services;
    }

    private static string? ResolveConfiguredInstanceId(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration[$"{SfidSettings.SectionName}:InstanceId"] ??
               configuration["ServiceRuntime:InstanceId"];
    }
}
