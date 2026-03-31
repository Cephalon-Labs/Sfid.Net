using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;

namespace SfidNet.Benchmark;

internal sealed class DefaultBenchmarkConfig : ManualConfig
{
    public DefaultBenchmarkConfig()
    {
        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumn(RankColumn.Arabic, StatisticColumn.Min, StatisticColumn.Max, StatisticColumn.P95);

        Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);
        SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
    }
}
