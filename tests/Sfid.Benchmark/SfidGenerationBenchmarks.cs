using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using SfidNet;
using SfidNet.Abstractions;

namespace SfidNet.Benchmark;

[Config(typeof(DefaultBenchmarkConfig))]
[SimpleJob(RuntimeMoniker.HostProcess, launchCount: 1, warmupCount: 5, iterationCount: 10)]
public class SfidGenerationBenchmarks
{
    private readonly SfidGenerator _generator = new(
        new SfidOptions
        {
            DatacenterId = 1,
            WorkerId = 1,
        });

    private ISfidGenerator? _previousRuntime;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _previousRuntime = SfidRuntime.Current;
        SfidRuntime.Bootstrap(
            new SfidOptions
            {
                DatacenterId = 2,
                WorkerId = 5,
            });
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        if (_previousRuntime is not null)
            SfidRuntime.UseGenerator(_previousRuntime);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Generation")]
    public long NextId()
        => _generator.NextId();

    [Benchmark]
    [BenchmarkCategory("Generation")]
    public Sfid NextSfid()
        => _generator.Next<Sfid>();

    [Benchmark]
    [BenchmarkCategory("Generation")]
    public OrderId NextTypedId()
        => _generator.Next<OrderId>();

    [Benchmark]
    [BenchmarkCategory("Runtime")]
    public long RuntimeNextId()
        => SfidRuntime.NextId();

    [Benchmark]
    [BenchmarkCategory("Runtime")]
    public Sfid RuntimeGenerate()
        => Sfid.Generate();
}
