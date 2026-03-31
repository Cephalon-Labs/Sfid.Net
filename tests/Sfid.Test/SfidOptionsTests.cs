using FluentAssertions;
using SfidNet;

namespace SfidNet.Test;

public sealed class SfidOptionsTests
{
    [Fact]
    public void Validate_ShouldAcceptBoundaryValues()
    {
        var options = new SfidOptions
        {
            CustomEpoch = DateTimeOffset.UtcNow.AddSeconds(-1),
            DatacenterId = SfidDefaults.MaxDatacenterId,
            WorkerId = SfidDefaults.MaxExpandedWorkerId,
            WorkerCapacity = SfidDefaults.MaxExpandedWorkerCapacity,
            ClockRegressionTolerance = TimeSpan.Zero,
        };

        var act = options.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(SfidDefaults.MaxExpandedWorkerCapacity + 1)]
    public void Validate_ShouldRejectInvalidWorkerCapacity(int workerCapacity)
    {
        var options = new SfidOptions
        {
            DatacenterId = 0,
            WorkerId = 0,
            WorkerCapacity = workerCapacity,
        };

        var act = options.Validate;

        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(SfidOptions.WorkerCapacity));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(SfidDefaults.MaxDatacenterId + 1)]
    public void Validate_ShouldRejectInvalidDatacenterId(int datacenterId)
    {
        var options = new SfidOptions
        {
            DatacenterId = datacenterId,
            WorkerId = 0,
        };

        var act = options.Validate;

        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(SfidOptions.DatacenterId));
    }

    [Theory]
    [InlineData(-1, SfidDefaults.DefaultWorkerCapacity)]
    [InlineData(SfidDefaults.DefaultWorkerCapacity, SfidDefaults.DefaultWorkerCapacity)]
    [InlineData(SfidDefaults.MaxExpandedWorkerCapacity, SfidDefaults.MaxExpandedWorkerCapacity)]
    public void Validate_ShouldRejectInvalidWorkerId(int workerId, int workerCapacity)
    {
        var options = new SfidOptions
        {
            DatacenterId = 0,
            WorkerId = workerId,
            WorkerCapacity = workerCapacity,
        };

        var act = options.Validate;

        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(SfidOptions.WorkerId));
    }

    [Fact]
    public void Validate_ShouldRejectFutureCustomEpoch()
    {
        var options = new SfidOptions
        {
            CustomEpoch = DateTimeOffset.UtcNow.AddMinutes(1),
        };

        var act = options.Validate;

        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(SfidOptions.CustomEpoch));
    }

    [Fact]
    public void Validate_ShouldRejectNegativeClockRegressionTolerance()
    {
        var options = new SfidOptions
        {
            ClockRegressionTolerance = TimeSpan.FromMilliseconds(-1),
        };

        var act = options.Validate;

        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(SfidOptions.ClockRegressionTolerance));
    }
}
