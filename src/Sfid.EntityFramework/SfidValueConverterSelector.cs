using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SfidNet.Abstractions;

namespace SfidNet.EntityFramework;

/// <summary>
/// Exposes default EF Core value converters for typed Sfid identifiers.
/// </summary>
public sealed class SfidValueConverterSelector : ValueConverterSelector
{
    /// <summary>
    /// Creates a new converter selector.
    /// </summary>
    public SfidValueConverterSelector(ValueConverterSelectorDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<ValueConverterInfo> Select(Type modelClrType, Type? providerClrType = null)
    {
        if (ImplementsTypedSfidInterface(modelClrType))
        {
            if (providerClrType is null || providerClrType == typeof(long))
            {
                yield return new ValueConverterInfo(
                    modelClrType,
                    typeof(long),
                    _ => (ValueConverter)Activator.CreateInstance(typeof(SfidToInt64Converter<>).MakeGenericType(modelClrType))!);
            }

            if (providerClrType is null || providerClrType == typeof(string))
            {
                yield return new ValueConverterInfo(
                    modelClrType,
                    typeof(string),
                    _ => (ValueConverter)Activator.CreateInstance(typeof(SfidToStringConverter<>).MakeGenericType(modelClrType))!);
            }
        }

        foreach (var converterInfo in base.Select(modelClrType, providerClrType))
            yield return converterInfo;
    }

    private static bool ImplementsTypedSfidInterface(Type identifierType)
        => identifierType
            .GetInterfaces()
            .Any(interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(ISfid<>) &&
                interfaceType.GenericTypeArguments[0] == identifierType);
}
