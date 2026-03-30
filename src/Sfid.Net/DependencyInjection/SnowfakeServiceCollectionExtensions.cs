using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sfid.Net.Abstractions;
using Sfid.Net;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers Snowfake runtime and generator services with the default .NET dependency injection container.
/// </summary>
public static class SnowfakeServiceCollectionExtensions
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

        var settings = configuration.GetSection(Sfid.Net.SnowfakeSettings.SectionName).Get<Sfid.Net.SnowfakeSettings>() ?? new Sfid.Net.SnowfakeSettings();
        var resolvedApplicationName = Sfid.Net.SnowfakeApplicationIdentityResolver.ResolveApplicationName(applicationName);
        var resolvedInstanceId = Sfid.Net.SnowfakeApplicationIdentityResolver.ResolveInstanceId(
            settings.InstanceId ?? instanceId,
            resolvedApplicationName);

        var nodeIdentity = Sfid.Net.SnowfakeNodeIdentityResolver.Resolve(settings, resolvedApplicationName, resolvedInstanceId);

        SfidRuntime.Bootstrap(new Sfid.Net.SfidOptions
        {
            CustomEpoch = settings.CustomEpoch ?? Sfid.Net.SfidDefaults.TwitterEpoch,
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

        return configuration[$"{Sfid.Net.SnowfakeSettings.SectionName}:InstanceId"] ??
               configuration["ServiceRuntime:InstanceId"];
    }
}
