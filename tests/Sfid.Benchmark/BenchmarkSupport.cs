using Sfid.Net.Abstractions;

namespace Sfid.Benchmark;

public readonly record struct OrderId(long Value) : ISfid<OrderId>
{
    public static OrderId FromInt64(long value)
        => new(value);
}
