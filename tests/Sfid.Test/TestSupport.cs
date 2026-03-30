using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Sfid.Net;
using Sfid.Net.Abstractions;
using System.Collections.Concurrent;

namespace Sfid.Test;

internal readonly record struct OrderId(long Value) : ISfid<OrderId>
{
    public static OrderId FromInt64(long value)
        => new(value);
}

internal sealed class AdjustableTimeProvider(DateTimeOffset initialValue) : TimeProvider
{
    private long _currentUnixTimeMilliseconds = initialValue.ToUnixTimeMilliseconds();

    public override DateTimeOffset GetUtcNow()
        => DateTimeOffset.FromUnixTimeMilliseconds(Interlocked.Read(ref _currentUnixTimeMilliseconds));

    public void Advance(TimeSpan duration)
        => Interlocked.Add(ref _currentUnixTimeMilliseconds, (long)duration.TotalMilliseconds);
}

internal sealed class ScriptedTimeProvider(IEnumerable<DateTimeOffset> timestamps) : TimeProvider
{
    private readonly ConcurrentQueue<long> _timestamps = new(timestamps.Select(timestamp => timestamp.ToUnixTimeMilliseconds()));
    private long _lastTimestamp = timestamps.LastOrDefault().ToUnixTimeMilliseconds();

    public ScriptedTimeProvider(params DateTimeOffset[] timestamps)
        : this(timestamps.AsEnumerable())
    {
    }

    public override DateTimeOffset GetUtcNow()
    {
        if (_timestamps.TryDequeue(out var next))
        {
            Interlocked.Exchange(ref _lastTimestamp, next);
            return DateTimeOffset.FromUnixTimeMilliseconds(next);
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(Interlocked.Read(ref _lastTimestamp));
    }
}

internal sealed class StubSfidGenerator(long startingValue = 1_000) : ISfidGenerator
{
    private long _currentValue = startingValue - 1;

    public long NextId()
        => Interlocked.Increment(ref _currentValue);

    public TId Next<TId>()
        where TId : struct, ISfid<TId>
        => TId.FromInt64(NextId());

    public SfidParts Decompose(long value)
        => new(
            value,
            DateTimeOffset.FromUnixTimeMilliseconds(value),
            DatacenterId: 0,
            WorkerId: 0,
            Sequence: 0);
}

internal sealed class RuntimeScope : IDisposable
{
    private readonly ISfidGenerator _previousGenerator = SfidRuntime.Current;

    public void Dispose()
        => SfidRuntime.UseGenerator(_previousGenerator);
}

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "app";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = default!;
}
