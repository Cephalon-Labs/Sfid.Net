using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SfidNet.Abstractions;
using System.Globalization;

namespace SfidNet.EntityFramework;

/// <summary>
/// Converts a typed Snowfake identifier to and from a string column.
/// </summary>
public sealed class SfidToStringConverter<TId> : ValueConverter<TId, string>
    where TId : struct, ISfid<TId>
{
    /// <summary>
    /// Creates a new converter instance.
    /// </summary>
    public SfidToStringConverter()
        : base(
            identifier => identifier.Value.ToString(CultureInfo.InvariantCulture),
            value => ConvertFromString(value))
    {
    }

    private static TId ConvertFromString(string value)
        => TId.FromInt64(long.Parse(value, CultureInfo.InvariantCulture));
}
