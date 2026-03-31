using SfidNet.Abstractions;

namespace SfidNet;

/// <summary>
/// Provides access to the process-wide Snowfake generator runtime.
/// </summary>
public static class SfidRuntime
{
    private static readonly object s_syncRoot = new();
    private static ISfidGenerator? s_current;

    /// <summary>
    /// Gets the current process-wide generator, creating a default instance when needed.
    /// </summary>
    public static ISfidGenerator Current
    {
        get
        {
            var generator = s_current;
            if (generator is not null)
                return generator;

            lock (s_syncRoot)
            {
                return s_current ??= CreateGenerator();
            }
        }
    }

    /// <summary>
    /// Bootstraps the process-wide generator from the supplied options.
    /// </summary>
    public static ISfidGenerator Bootstrap(SfidOptions? options = null, TimeProvider? timeProvider = null)
        => UseGenerator(CreateGenerator(options, timeProvider));

    /// <summary>
    /// Replaces the process-wide generator with the supplied implementation.
    /// </summary>
    public static ISfidGenerator UseGenerator(ISfidGenerator generator)
    {
        ArgumentNullException.ThrowIfNull(generator);

        lock (s_syncRoot)
        {
            s_current = generator;
            return s_current;
        }
    }

    /// <summary>
    /// Generates the next raw Snowfake value.
    /// </summary>
    public static long NextId()
        => Current.NextId();

    /// <summary>
    /// Generates the next strongly typed identifier.
    /// </summary>
    public static TId Next<TId>()
        where TId : struct, ISfid<TId>
        => Current.Next<TId>();

    private static ISfidGenerator CreateGenerator(SfidOptions? options = null, TimeProvider? timeProvider = null)
        => new SfidGenerator(options ?? new SfidOptions(), timeProvider);
}
