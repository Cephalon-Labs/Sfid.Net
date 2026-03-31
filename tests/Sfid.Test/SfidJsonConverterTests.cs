using FluentAssertions;
using SfidNet;
using SfidNet.Serialization;
using System.Globalization;
using System.Text.Json;

namespace SfidNet.Test;

public sealed class SfidJsonConverterTests
{
    [Fact]
    public void SfidJsonConverter_ShouldSerializeSfidAsJsonString()
    {
        var json = JsonSerializer.Serialize(new SfidEnvelope(new Sfid(42)));

        json.Should().Be("{\"Id\":\"42\"}");
    }

    [Theory]
    [InlineData("{\"Id\":\"42\"}")]
    [InlineData("{\"Id\":42}")]
    public void SfidJsonConverter_ShouldDeserializeSfidFromStringOrNumber(string json)
    {
        var envelope = JsonSerializer.Deserialize<SfidEnvelope>(json);

        envelope.Should().NotBeNull();
        envelope!.Id.Should().Be(new Sfid(42));
    }

    [Fact]
    public void SfidJsonConverterFactory_ShouldSerializeTypedIdentifierAsJsonString()
    {
        var options = CreateOptions();

        var json = JsonSerializer.Serialize(new OrderEnvelope(new OrderId(99)), options);

        json.Should().Be("{\"Id\":\"99\"}");
    }

    [Theory]
    [InlineData("{\"Id\":\"99\"}")]
    [InlineData("{\"Id\":99}")]
    public void SfidJsonConverterFactory_ShouldDeserializeTypedIdentifierFromStringOrNumber(string json)
    {
        var options = CreateOptions();

        var envelope = JsonSerializer.Deserialize<OrderEnvelope>(json, options);

        envelope.Should().NotBeNull();
        envelope!.Id.Should().Be(new OrderId(99));
    }

    [Fact]
    public void SfidJsonConverterFactory_ShouldThrowForInvalidString()
    {
        var options = CreateOptions();

        var act = () => JsonSerializer.Deserialize<OrderEnvelope>("{\"Id\":\"nope\"}", options);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void SfidJsonConverter_ShouldThrowForNullToken()
    {
        var act = () => JsonSerializer.Deserialize<SfidEnvelope>("{\"Id\":null}");

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void SfidJsonConverter_ShouldThrowForUnexpectedToken()
    {
        var act = () => JsonSerializer.Deserialize<SfidEnvelope>("{\"Id\":{}}");

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void SfidJsonConverterFactory_ShouldDetectSupportedTypes()
    {
        var factory = new SfidJsonConverterFactory();

        factory.CanConvert(typeof(Sfid)).Should().BeTrue();
        factory.CanConvert(typeof(OrderId)).Should().BeTrue();
        factory.CanConvert(typeof(string)).Should().BeFalse();
    }

    [Fact]
    public void Parse_ShouldSupportBindingFriendlyOverloads()
    {
        Sfid.Parse("321").Should().Be(new Sfid(321));
        Sfid.Parse("321", CultureInfo.InvariantCulture).Should().Be(new Sfid(321));
        Sfid.TryParse("321", CultureInfo.InvariantCulture, out var parsed).Should().BeTrue();
        parsed.Should().Be(new Sfid(321));
    }

    [Fact]
    public void TryParse_ShouldParseValidValuesForRouteAndQueryBinding()
    {
        var result = Sfid.TryParse("123456789", out var sfid);

        result.Should().BeTrue();
        sfid.Should().Be(new Sfid(123456789));
    }

    [Fact]
    public void TryParse_ShouldRejectInvalidValuesForRouteAndQueryBinding()
    {
        var result = Sfid.TryParse("not-a-number", out var sfid);

        result.Should().BeFalse();
        sfid.Should().Be(default(Sfid));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new SfidJsonConverterFactory());
        return options;
    }

    private sealed record SfidEnvelope(Sfid Id);

    private sealed record OrderEnvelope(OrderId Id);
}
