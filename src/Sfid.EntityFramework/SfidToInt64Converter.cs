using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SfidNet.Abstractions;

namespace SfidNet.EntityFramework;

/// <summary>
/// Converts a typed Snowfake identifier to and from a 64-bit integer column.
/// </summary>
public sealed class SfidToInt64Converter<TId> : ValueConverter<TId, long>
    where TId : struct, ISfid<TId>
{
    /// <summary>
    /// Creates a new converter instance.
    /// </summary>
    public SfidToInt64Converter()
        : base(identifier => identifier.Value, value => ConvertFromInt64(value))
    {
    }

    private static TId ConvertFromInt64(long value)
        => TId.FromInt64(value);
}
