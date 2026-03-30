namespace Sfid.Net;

internal readonly record struct SfidLayout(
    int WorkerBits,
    int SequenceBits,
    int WorkerShift,
    int DatacenterShift,
    int TimestampShift,
    int WorkerMask,
    int SequenceMask,
    int MaxSequence)
{
    public static SfidLayout FromOptions(SfidOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var workerBits = Math.Max(SfidDefaults.WorkerBits, GetRequiredBits(options.WorkerCapacity - 1));
        var borrowedSequenceBits = workerBits - SfidDefaults.WorkerBits;
        var sequenceBits = SfidDefaults.SequenceBits - borrowedSequenceBits;

        if (sequenceBits < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.WorkerCapacity),
                options.WorkerCapacity,
                $"WorkerCapacity exceeds the supported maximum of {SfidDefaults.MaxExpandedWorkerCapacity}.");
        }

        var workerShift = sequenceBits;
        var datacenterShift = SfidDefaults.SequenceBits + SfidDefaults.WorkerBits;
        var timestampShift = datacenterShift + SfidDefaults.DatacenterBits;
        var workerMask = (1 << workerBits) - 1;
        var sequenceMask = sequenceBits == 0 ? 0 : (1 << sequenceBits) - 1;

        return new SfidLayout(
            workerBits,
            sequenceBits,
            workerShift,
            datacenterShift,
            timestampShift,
            workerMask,
            sequenceMask,
            sequenceMask);
    }

    private static int GetRequiredBits(int maxValue)
    {
        if (maxValue <= 0)
            return 1;

        var bits = 0;
        var value = maxValue;
        while (value > 0)
        {
            bits++;
            value >>= 1;
        }

        return bits;
    }
}
