using SfidNet.Abstractions;

namespace SfidNet;

/// <summary>
/// Generates Twitter Snowflake-compatible 64-bit identifiers for clustered systems.
/// </summary>
public sealed class SfidGenerator : ISfidGenerator
{
    private readonly object _gate = new();
    private readonly SfidLayout _layout;
    private readonly SfidOptions _options;
    private readonly TimeProvider _timeProvider;
    private long _lastTimestamp = -1;
    private int _sequence;

    /// <summary>
    /// Creates a new generator for the configured cluster node.
    /// </summary>
    public SfidGenerator(SfidOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        _options = options;
        _layout = SfidLayout.FromOptions(options);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Generates the next raw 64-bit identifier.
    /// </summary>
    public long NextId()
    {
        lock (_gate)
        {
            var currentTimestamp = GetCurrentTimestampMilliseconds();

            if (currentTimestamp < _lastTimestamp)
            {
                var drift = _lastTimestamp - currentTimestamp;
                if (drift > (long)_options.ClockRegressionTolerance.TotalMilliseconds)
                {
                    throw new InvalidOperationException(
                        $"System clock moved backwards by {drift}ms, which exceeds the configured tolerance.");
                }

                currentTimestamp = WaitUntilNextTimestamp(_lastTimestamp);
            }

            if (currentTimestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & _layout.MaxSequence;
                if (_sequence == 0)
                {
                    currentTimestamp = WaitUntilNextTimestamp(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = currentTimestamp;
            return Compose(currentTimestamp, _options.DatacenterId, _options.WorkerId, _sequence);
        }
    }

    /// <summary>
    /// Generates the next strongly typed identifier.
    /// </summary>
    public TId Next<TId>()
        where TId : struct, ISfid<TId>
        => TId.FromInt64(NextId());

    /// <summary>
    /// Decomposes a raw identifier into timestamp, node, and sequence parts.
    /// </summary>
    public SfidParts Decompose(long value)
    {
        var sequence = (int)(value & _layout.SequenceMask);
        var workerId = (int)((value >> _layout.WorkerShift) & _layout.WorkerMask);
        var datacenterId = (int)((value >> _layout.DatacenterShift) & SfidDefaults.MaxDatacenterId);
        var timestampPart = value >> _layout.TimestampShift;
        var timestamp = _options.CustomEpoch.AddMilliseconds(timestampPart);

        return new SfidParts(value, timestamp, datacenterId, workerId, sequence);
    }

    private long Compose(long currentTimestamp, int datacenterId, int workerId, int sequence)
    {
        var timestampPart = currentTimestamp - _options.CustomEpoch.ToUnixTimeMilliseconds();
        if (timestampPart < 0)
        {
            throw new InvalidOperationException("The current timestamp is before the configured custom epoch.");
        }

        return (timestampPart << _layout.TimestampShift) |
               ((long)datacenterId << _layout.DatacenterShift) |
               ((long)workerId << _layout.WorkerShift) |
               (uint)sequence;
    }

    private long GetCurrentTimestampMilliseconds()
        => _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

    private long WaitUntilNextTimestamp(long lastTimestamp)
    {
        var currentTimestamp = GetCurrentTimestampMilliseconds();
        while (currentTimestamp <= lastTimestamp)
        {
            Thread.SpinWait(16);
            currentTimestamp = GetCurrentTimestampMilliseconds();
        }

        return currentTimestamp;
    }
}
