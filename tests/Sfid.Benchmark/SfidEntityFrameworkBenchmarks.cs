using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Sfid.EntityFramework;

namespace Sfid.Benchmark;

[Config(typeof(DefaultBenchmarkConfig))]
[SimpleJob(RuntimeMoniker.HostProcess, launchCount: 1, warmupCount: 5, iterationCount: 10)]
public class SfidEntityFrameworkBenchmarks
{
    private readonly OrderId _identifier = new(123456789012345678);
    private readonly long _rawInt64 = 123456789012345678;
    private readonly string _rawString = "123456789012345678";

    private readonly Func<OrderId, long> _toInt64 = new SfidToInt64Converter<OrderId>().ConvertToProviderExpression.Compile();
    private readonly Func<long, OrderId> _fromInt64 = new SfidToInt64Converter<OrderId>().ConvertFromProviderExpression.Compile();
    private readonly Func<OrderId, string> _toString = new SfidToStringConverter<OrderId>().ConvertToProviderExpression.Compile();
    private readonly Func<string, OrderId> _fromString = new SfidToStringConverter<OrderId>().ConvertFromProviderExpression.Compile();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("EF Conversion")]
    public long ConvertToInt64()
        => _toInt64(_identifier);

    [Benchmark]
    [BenchmarkCategory("EF Conversion")]
    public OrderId ConvertFromInt64()
        => _fromInt64(_rawInt64);

    [Benchmark]
    [BenchmarkCategory("EF Conversion")]
    public string ConvertToString()
        => _toString(_identifier);

    [Benchmark]
    [BenchmarkCategory("EF Conversion")]
    public OrderId ConvertFromString()
        => _fromString(_rawString);
}
