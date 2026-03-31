using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SfidNet;
using SfidNet.Abstractions;

namespace SfidNet.Test;

public sealed class SfidDependencyInjectionTests
{
    [Fact]
    public void AddSnowfake_ShouldBootstrapRuntimeFromConfigurationAndEnvironment()
    {
        using var _ = new RuntimeScope();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("Snowfake:DatacenterId", "4"),
                new KeyValuePair<string, string?>("Snowfake:WorkerCapacity", "1024"),
                new KeyValuePair<string, string?>("ServiceRuntime:InstanceId", "api-42"),
            ])
            .Build();

        var services = new ServiceCollection();
        services.AddSnowfake(
            configuration,
            new TestHostEnvironment
            {
                ApplicationName = "Neza.Api",
            });

        using var provider = services.BuildServiceProvider();
        var generator = provider.GetRequiredService<ISfidGenerator>();
        var parts = generator.Decompose(generator.NextId());

        generator.Should().BeSameAs(SfidRuntime.Current);
        parts.DatacenterId.Should().Be(4);
        parts.WorkerId.Should().BeInRange(0, 1023);
    }

    [Fact]
    public void AddSnowfake_ShouldSupportManualApplicationMetadata()
    {
        using var _ = new RuntimeScope();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("Snowfake:DatacenterId", "9"),
                new KeyValuePair<string, string?>("Snowfake:WorkerId", "2048"),
                new KeyValuePair<string, string?>("Snowfake:WorkerCapacity", "4096"),
            ])
            .Build();

        var services = new ServiceCollection();
        services.AddSnowfake(configuration, "orders-api", "orders-api-7");

        using var provider = services.BuildServiceProvider();
        var generator = provider.GetRequiredService<ISfidGenerator>();
        var parts = generator.Decompose(generator.NextId());

        parts.DatacenterId.Should().Be(9);
        parts.WorkerId.Should().Be(2048);
    }

    [Fact]
    public void AddSnowfake_ShouldSupportCodeBasedConfiguration()
    {
        using var _ = new RuntimeScope();
        var services = new ServiceCollection();
        services.AddSnowfake(options =>
        {
            options.DatacenterId = 2;
            options.WorkerId = 55;
            options.WorkerCapacity = 64;
        });

        using var provider = services.BuildServiceProvider();
        var generator = provider.GetRequiredService<ISfidGenerator>();
        var parts = generator.Decompose(generator.NextId());

        parts.DatacenterId.Should().Be(2);
        parts.WorkerId.Should().Be(55);
    }

    [Fact]
    public void AddSnowfake_ShouldPreferConfiguredInstanceIdOverFallbackInstanceId()
    {
        using var _ = new RuntimeScope();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("Snowfake:DatacenterId", "7"),
                new KeyValuePair<string, string?>("Snowfake:InstanceId", "stable-instance"),
            ])
            .Build();

        var firstWorkerId = ResolveWorkerId(configuration, fallbackInstanceId: "ignored-1");
        var secondWorkerId = ResolveWorkerId(configuration, fallbackInstanceId: "ignored-2");

        firstWorkerId.Should().Be(secondWorkerId);
    }

    [Fact]
    public void AddSnowfake_ShouldDeriveStableNodeIdentityWhenValuesAreAutoAssigned()
    {
        using var _ = new RuntimeScope();
        var configuration = new ConfigurationBuilder().Build();

        var firstParts = ResolveGeneratedParts(configuration, "Orders.Api", "orders-api-01");
        var secondParts = ResolveGeneratedParts(configuration, "Orders.Api", "orders-api-01");

        firstParts.DatacenterId.Should().Be(secondParts.DatacenterId);
        firstParts.WorkerId.Should().Be(secondParts.WorkerId);
        firstParts.DatacenterId.Should().BeInRange(0, SfidDefaults.MaxDatacenterId);
        firstParts.WorkerId.Should().BeInRange(0, SfidDefaults.MaxWorkerId);
    }

    [Fact]
    public void AddSnowfake_ShouldThrowForNullEnvironment()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddSnowfake(configuration, environment: null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddSnowfake_ShouldThrowForNullConfigureDelegate()
    {
        var services = new ServiceCollection();

        var act = () => services.AddSnowfake(configure: null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static int ResolveWorkerId(IConfiguration configuration, string fallbackInstanceId)
        => ResolveGeneratedParts(configuration, "Orders.Api", fallbackInstanceId).WorkerId;

    private static SfidParts ResolveGeneratedParts(IConfiguration configuration, string applicationName, string instanceId)
    {
        var services = new ServiceCollection();
        services.AddSnowfake(configuration, applicationName, instanceId);

        using var provider = services.BuildServiceProvider();
        var generator = provider.GetRequiredService<ISfidGenerator>();
        return generator.Decompose(generator.NextId());
    }
}
