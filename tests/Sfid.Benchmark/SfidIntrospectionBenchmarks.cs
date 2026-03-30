using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Sfid.Net;
using System.Globalization;

namespace Sfid.Benchmark;

[Config(typeof(DefaultBenchmarkConfig))]
[SimpleJob(RuntimeMoniker.HostProcess, launchCount: 1, warmupCount: 5, iterationCount: 10)]
public class SfidIntrospectionBenchmarks
{
    private readonly SfidGenerator _generator = new(
        new SfidOptions
        {
            DatacenterId = 3,
            WorkerId = 7,
        });

    private long _rawValue;
    private string _rawText = string.Empty;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _rawValue = _generator.NextId();
        _rawText = _rawValue.ToString(CultureInfo.InvariantCulture);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Inspection")]
    public SfidParts Decompose()
        => _generator.Decompose(_rawValue);

    [Benchmark]
    [BenchmarkCategory("Parsing")]
    public global::Sfid.Net.Sfid FromInt64ToSfid()
        => SfidParser.FromInt64<global::Sfid.Net.Sfid>(_rawValue);

    [Benchmark]
    [BenchmarkCategory("Parsing")]
    public OrderId FromInt64ToTypedId()
        => SfidParser.FromInt64<OrderId>(_rawValue);

    [Benchmark]
    [BenchmarkCategory("Parsing")]
    public OrderId ParseTypedId()
        => SfidParser.Parse<OrderId>(_rawText);

    [Benchmark]
    [BenchmarkCategory("Parsing")]
    public bool TryParseTypedId()
        => SfidParser.TryParse<OrderId>(_rawText, out _);
}
