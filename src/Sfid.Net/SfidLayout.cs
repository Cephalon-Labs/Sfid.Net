namespace SfidNet;

/// <summary>
/// Represents the bit layout configuration for a Snowflake-inspired ID generator, defining how worker, sequence,
/// datacenter, and timestamp components are encoded within the generated identifier.
/// </summary>
/// <remarks>This struct is used internally to calculate and enforce the bitwise structure of generated IDs,
/// ensuring that each component fits within its designated bit range. The layout is determined based on configuration
/// options and system defaults, and is critical for correct ID generation and parsing.</remarks>
/// <param name="WorkerBits">The number of bits allocated for the worker identifier within the ID.</param>
/// <param name="SequenceBits">The number of bits allocated for the sequence number within the ID.</param>
/// <param name="WorkerShift">The number of bits to shift the worker identifier to its position in the ID.</param>
/// <param name="DatacenterShift">The number of bits to shift the datacenter identifier to its position in the ID.</param>
/// <param name="TimestampShift">The number of bits to shift the timestamp to its position in the ID.</param>
/// <param name="WorkerMask">The bitmask used to extract or limit the worker identifier value.</param>
/// <param name="SequenceMask">The bitmask used to extract or limit the sequence number value.</param>
/// <param name="MaxSequence">The maximum value allowed for the sequence number in a single timestamp interval.</param>
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

    /// <summary>
    /// Creates a new instance of the SfidLayout class based on the specified options.
    /// </summary>
    /// <remarks>This method adjusts the allocation of worker and sequence bits to accommodate the specified
    /// WorkerCapacity. If the requested capacity requires more bits than supported, an exception is thrown.</remarks>
    /// <param name="options">The configuration options that define the worker and sequence bit allocation. Cannot be null.</param>
    /// <returns>A SfidLayout instance configured according to the provided options.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the WorkerCapacity specified in options exceeds the supported maximum.</exception>
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
