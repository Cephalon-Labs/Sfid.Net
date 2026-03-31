using FluentAssertions;
using SfidNet;

namespace SfidNet.Test;

public sealed class SfidGeneratorTests
{
    [Fact]
    public void Sfid_ShouldRoundTripRawValue()
    {
        var identifier = global::SfidNet.Sfid.FromInt64(1234567890);

        identifier.Value.Should().Be(1234567890);
        identifier.ToString().Should().Be("1234567890");
    }

    [Fact]
    public void NextId_ShouldProduceAscendingValues()
    {
        var generator = CreateGenerator(datacenterId: 1, workerId: 3);

        var values = Enumerable.Range(0, 512)
            .Select(_ => generator.NextId())
            .ToArray();

        values.Should().OnlyHaveUniqueItems();
        values.Should().BeInAscendingOrder();
    }

    [Fact]
    public void Next_ShouldProduceTypedIdentifier()
    {
        var generator = CreateGenerator(datacenterId: 1, workerId: 7);

        var identifier = generator.Next<OrderId>();

        identifier.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task NextId_ShouldRemainUniqueAcrossConcurrentCalls()
    {
        var generator = CreateGenerator(datacenterId: 2, workerId: 9);
        var values = new long[10_000];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, values.Length),
            async (index, _) =>
            {
                values[index] = generator.NextId();
                await Task.CompletedTask;
            });

        values.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void NextId_ShouldRemainUniqueAcrossClusterNodes()
    {
        var leftGenerator = CreateGenerator(datacenterId: 3, workerId: 1);
        var rightGenerator = CreateGenerator(datacenterId: 3, workerId: 2);

        var leftValue = leftGenerator.NextId();
        var rightValue = rightGenerator.NextId();

        leftValue.Should().NotBe(rightValue);

        var leftParts = leftGenerator.Decompose(leftValue);
        var rightParts = rightGenerator.Decompose(rightValue);

        leftParts.DatacenterId.Should().Be(3);
        leftParts.WorkerId.Should().Be(1);
        rightParts.DatacenterId.Should().Be(3);
        rightParts.WorkerId.Should().Be(2);
    }

    [Fact]
    public void Decompose_ShouldReturnTimestampNodeAndSequence()
    {
        var fixedTime = new AdjustableTimeProvider(DateTimeOffset.Parse("2026-03-18T00:00:00Z"));
        var generator = CreateGenerator(datacenterId: 4, workerId: 5, timeProvider: fixedTime);

        var firstValue = generator.NextId();
        var secondValue = generator.NextId();

        var firstParts = generator.Decompose(firstValue);
        var secondParts = generator.Decompose(secondValue);

        firstParts.Timestamp.Should().Be(fixedTime.GetUtcNow());
        firstParts.DatacenterId.Should().Be(4);
        firstParts.WorkerId.Should().Be(5);
        firstParts.Sequence.Should().Be(0);
        secondParts.Sequence.Should().Be(1);
    }

    [Fact]
    public void Decompose_ShouldSupportExpandedWorkerCapacity()
    {
        var fixedTime = new AdjustableTimeProvider(DateTimeOffset.Parse("2026-03-18T05:00:00Z"));
        var generator = CreateGenerator(datacenterId: 4, workerId: 777, workerCapacity: 1024, timeProvider: fixedTime);

        var firstValue = generator.NextId();
        var secondValue = generator.NextId();

        var firstParts = generator.Decompose(firstValue);
        var secondParts = generator.Decompose(secondValue);

        firstParts.Timestamp.Should().Be(fixedTime.GetUtcNow());
        firstParts.DatacenterId.Should().Be(4);
        firstParts.WorkerId.Should().Be(777);
        firstParts.Sequence.Should().Be(0);
        secondParts.WorkerId.Should().Be(777);
        secondParts.Sequence.Should().Be(1);
    }

    [Fact]
    public void NextId_ShouldRemainUniqueAcrossExpandedWorkerSlots()
    {
        var leftGenerator = CreateGenerator(datacenterId: 3, workerId: 511, workerCapacity: 1024);
        var rightGenerator = CreateGenerator(datacenterId: 3, workerId: 512, workerCapacity: 1024);

        var leftValue = leftGenerator.NextId();
        var rightValue = rightGenerator.NextId();

        leftValue.Should().NotBe(rightValue);

        var leftParts = leftGenerator.Decompose(leftValue);
        var rightParts = rightGenerator.Decompose(rightValue);

        leftParts.DatacenterId.Should().Be(3);
        leftParts.WorkerId.Should().Be(511);
        rightParts.DatacenterId.Should().Be(3);
        rightParts.WorkerId.Should().Be(512);
    }

    [Fact]
    public void NextId_ShouldWaitForNextTimestamp_WhenSequenceBitsAreExhausted()
    {
        var start = DateTimeOffset.Parse("2026-03-18T06:30:00Z");
        var timeProvider = new ScriptedTimeProvider(
            start,
            start,
            start,
            start.AddMilliseconds(1));

        var generator = CreateGenerator(
            datacenterId: 5,
            workerId: SfidDefaults.MaxExpandedWorkerId,
            workerCapacity: SfidDefaults.MaxExpandedWorkerCapacity,
            timeProvider: timeProvider);

        var firstValue = generator.NextId();
        var secondValue = generator.NextId();

        var firstParts = generator.Decompose(firstValue);
        var secondParts = generator.Decompose(secondValue);

        firstParts.Sequence.Should().Be(0);
        secondParts.Timestamp.Should().Be(start.AddMilliseconds(1));
        secondParts.Sequence.Should().Be(0);
        secondValue.Should().BeGreaterThan(firstValue);
    }

    [Fact]
    public void NextId_ShouldWaitWhenClockRegressionIsWithinTolerance()
    {
        var start = DateTimeOffset.Parse("2026-03-18T06:45:00Z");
        var timeProvider = new ScriptedTimeProvider(
            start,
            start.AddMilliseconds(-1),
            start.AddMilliseconds(-1),
            start.AddMilliseconds(1));

        var generator = new SfidGenerator(
            new SfidOptions
            {
                DatacenterId = 1,
                WorkerId = 1,
                ClockRegressionTolerance = TimeSpan.FromMilliseconds(5),
            },
            timeProvider);

        var firstValue = generator.NextId();
        var secondValue = generator.NextId();
        var secondParts = generator.Decompose(secondValue);

        secondParts.Timestamp.Should().Be(start.AddMilliseconds(1));
        secondParts.Sequence.Should().Be(0);
        secondValue.Should().BeGreaterThan(firstValue);
    }

    [Fact]
    public void NextId_ShouldThrowWhenClockRegressionExceedsTolerance()
    {
        var start = DateTimeOffset.Parse("2026-03-18T07:00:00Z");
        var timeProvider = new ScriptedTimeProvider(
            start,
            start.AddMilliseconds(-10));

        var generator = new SfidGenerator(
            new SfidOptions
            {
                DatacenterId = 1,
                WorkerId = 1,
                ClockRegressionTolerance = TimeSpan.FromMilliseconds(2),
            },
            timeProvider);

        generator.NextId();

        var act = generator.NextId;

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("System clock moved backwards by 10ms*");
    }

    [Fact]
    public void NextId_ShouldThrowWhenTimeProviderPrecedesCustomEpoch()
    {
        var customEpoch = DateTimeOffset.UtcNow.AddMinutes(-1);
        var timeProvider = new AdjustableTimeProvider(customEpoch.AddSeconds(-1));
        var generator = new SfidGenerator(
            new SfidOptions
            {
                CustomEpoch = customEpoch,
                DatacenterId = 1,
                WorkerId = 1,
            },
            timeProvider);

        var act = generator.NextId;

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("The current timestamp is before the configured custom epoch.");
    }

    private static SfidGenerator CreateGenerator(
        int datacenterId,
        int workerId,
        int workerCapacity = SfidDefaults.DefaultWorkerCapacity,
        TimeProvider? timeProvider = null)
    {
        return new SfidGenerator(
            new SfidOptions
            {
                DatacenterId = datacenterId,
                WorkerId = workerId,
                WorkerCapacity = workerCapacity,
            },
            timeProvider);
    }
}
