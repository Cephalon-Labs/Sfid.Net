using FluentAssertions;
using Sfid.Net;

namespace Sfid.Test;

public sealed class SfidParserAndRuntimeTests
{
    [Fact]
    public void FromInt64_ShouldCreateTypedIdentifier()
    {
        var identifier = SfidParser.FromInt64<OrderId>(9876543210);

        identifier.Should().Be(new OrderId(9876543210));
    }

    [Fact]
    public void Parse_ShouldCreateTypedIdentifier()
    {
        var identifier = SfidParser.Parse<OrderId>("123456789012345678");

        identifier.Should().Be(new OrderId(123456789012345678));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Parse_ShouldThrowForBlankValues(string? value)
    {
        var act = () => SfidParser.Parse<OrderId>(value!);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Value must not be empty.*");
    }

    [Fact]
    public void TryParse_ShouldReturnFalseForInvalidInput()
    {
        var result = SfidParser.TryParse<OrderId>("not-a-number", out var identifier);

        result.Should().BeFalse();
        identifier.Should().Be(default(OrderId));
    }

    [Fact]
    public void Bootstrap_ShouldUseConfiguredGeneratorForRuntimeApis()
    {
        using var _ = new RuntimeScope();
        var fixedTime = new AdjustableTimeProvider(DateTimeOffset.Parse("2026-03-18T01:02:03Z"));

        var runtimeGenerator = (SfidGenerator)SfidRuntime.Bootstrap(
            new SfidOptions
            {
                DatacenterId = 6,
                WorkerId = 11,
            },
            fixedTime);

        var rawIdentifier = SfidRuntime.NextId();
        var typedIdentifier = SfidRuntime.Next<OrderId>();
        var defaultIdentifier = global::Sfid.Net.Sfid.Generate();

        var rawParts = runtimeGenerator.Decompose(rawIdentifier);
        var typedParts = runtimeGenerator.Decompose(typedIdentifier.Value);
        var defaultParts = runtimeGenerator.Decompose(defaultIdentifier.Value);

        rawParts.Timestamp.Should().Be(fixedTime.GetUtcNow());
        rawParts.DatacenterId.Should().Be(6);
        rawParts.WorkerId.Should().Be(11);
        rawParts.Sequence.Should().Be(0);

        typedParts.Sequence.Should().Be(1);
        defaultParts.Sequence.Should().Be(2);
    }

    [Fact]
    public void UseGenerator_ShouldReplaceCurrentGenerator()
    {
        using var _ = new RuntimeScope();
        var generator = new StubSfidGenerator(500);

        var current = SfidRuntime.UseGenerator(generator);

        current.Should().BeSameAs(generator);
        SfidRuntime.Current.Should().BeSameAs(generator);
        SfidRuntime.NextId().Should().Be(500);
        SfidRuntime.Next<OrderId>().Should().Be(new OrderId(501));
    }

    [Fact]
    public void UseGenerator_ShouldThrowForNullGenerator()
    {
        var act = () => SfidRuntime.UseGenerator(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
