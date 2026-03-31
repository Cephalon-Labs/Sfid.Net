using SfidNet.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SfidNet.Serialization;

/// <summary>
/// Creates JSON converters for <see cref="T:SfidNet.Sfid"/> and strongly typed <see cref="ISfid{TSelf}"/> values.
/// </summary>
public sealed class SfidJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert == typeof(Sfid))
            return true;

        return typeToConvert
            .GetInterfaces()
            .Any(interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(ISfid<>) &&
                interfaceType.GenericTypeArguments[0] == typeToConvert);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(Sfid))
            return new SfidJsonConverter();

        var converterType = typeof(SfidJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
