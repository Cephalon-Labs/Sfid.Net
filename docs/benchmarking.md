# Benchmarking Guide

## Scope

The benchmark project measures the hot paths that matter most when evaluating `Sfid.Net` in real applications:

- ID generation through a dedicated `SfidGenerator`
- process-wide runtime generation through `SfidRuntime`
- parsing and decomposition paths
- EF Core converter hot paths for `long` and `string` storage

## Benchmark Suites

- `SfidGenerationBenchmarks`
  Measures `NextId()`, `Next<Sfid>()`, `Next<OrderId>()`, `SfidRuntime.NextId()`, and `Sfid.Generate()`.
- `SfidIntrospectionBenchmarks`
  Measures `Decompose()`, `FromInt64()`, `Parse()`, and `TryParse()`.
- `SfidEntityFrameworkBenchmarks`
  Measures converter round-trips for `long` and `string` storage.

All benchmark classes use BenchmarkDotNet with:

- memory diagnostics enabled
- GitHub-flavored Markdown export
- CSV export
- min, max, p95, and ratio columns
- fixed warmup and iteration counts for stable local comparisons

## Run the Benchmarks

```bash
dotnet run --project tests/Sfid.Benchmark/Sfid.Benchmark.csproj -c Release -- --filter *
```

To run a single suite:

```bash
dotnet run --project tests/Sfid.Benchmark/Sfid.Benchmark.csproj -c Release -- --filter *SfidGenerationBenchmarks*
```

BenchmarkDotNet writes the exported summaries to `BenchmarkDotNet.Artifacts/results/`.

## How to Read the Results

- `Mean` is the average execution time.
- `P95` helps show tail latency for each microbenchmark.
- `Allocated` should remain at or near zero for generation and parsing paths.
- `Ratio` compares each benchmark with the suite baseline.

Generation benchmarks answer “how expensive is ID creation on the hot path?” while parsing and EF benchmarks answer “how much overhead do typed IDs and persistence helpers add around that core path?”

## Latest Recorded Run

The current repository snapshot records the latest benchmark results in [`verification.md`](verification.md), including the exact command and timestamp used to produce them.
