# Validation Snapshot

This document records the latest automated test and benchmark results captured from the repository workspace.

## Test Run

- Timestamp: `2026-03-31 05:12:12 +07:00`
- Command:

  ```bash
  dotnet test tests/Sfid.Test/Sfid.Test.csproj -c Release --collect:"XPlat Code Coverage" --results-directory artifacts/test-results
  ```

- Result: `67 passed`, `0 failed`, `0 skipped`
- Duration: `699 ms`
- Coverage report: `artifacts/test-results/11a12cca-b218-4b7c-bfc0-dedc7262c0f8/coverage.cobertura.xml`
- Line coverage: `92.5%` (`368 / 398`)
- Branch coverage: `79.9%` (`147 / 184`)

## Benchmark Run

- Timestamp: `2026-03-31 02:27:14 +07:00`
- Command:

  ```bash
  dotnet run --project tests/Sfid.Benchmark/Sfid.Benchmark.csproj -c Release -- --filter *
  ```

- Runtime: `.NET 10.0.5`
- SDK: `.NET SDK 10.0.201`
- Machine: `13th Gen Intel Core i5-13500`
- OS: `Windows 11`
- Total benchmark execution time: approximately `4m 18s`

### Generation Benchmarks

| Method | Mean | P95 | Allocated |
| --- | ---: | ---: | ---: |
| `RuntimeGenerate` | `242.1 ns` | `242.2 ns` | `0 B` |
| `NextTypedId` | `242.2 ns` | `242.2 ns` | `0 B` |
| `NextId` | `242.2 ns` | `242.3 ns` | `0 B` |
| `RuntimeNextId` | `242.2 ns` | `242.3 ns` | `0 B` |
| `NextSfid` | `242.2 ns` | `242.3 ns` | `0 B` |

### Introspection Benchmarks

| Method | Mean | P95 | Allocated |
| --- | ---: | ---: | ---: |
| `FromInt64ToTypedId` | `0.0039 ns` | `0.0066 ns` | `0 B` |
| `FromInt64ToSfid` | `0.0057 ns` | `0.0116 ns` | `0 B` |
| `Decompose` | `2.7850 ns` | `2.7981 ns` | `0 B` |
| `TryParseTypedId` | `14.1387 ns` | `14.3185 ns` | `0 B` |
| `ParseTypedId` | `14.8186 ns` | `14.9621 ns` | `0 B` |

### Entity Framework Conversion Benchmarks

| Method | Mean | P95 | Allocated |
| --- | ---: | ---: | ---: |
| `ConvertFromInt64` | `0.3362 ns` | `0.3444 ns` | `0 B` |
| `ConvertToInt64` | `0.3519 ns` | `0.3577 ns` | `0 B` |
| `ConvertToString` | `12.1078 ns` | `12.3972 ns` | `64 B` |
| `ConvertFromString` | `13.9594 ns` | `14.3191 ns` | `0 B` |

## Notes

- `FromInt64ToSfid` and `FromInt64ToTypedId` are effectively wrapper constructions, so BenchmarkDotNet reports them as near-zero work and flags them as zero-measurement cases.
- The generation path remained allocation-free across all measured variants.
- The main allocation observed in the benchmark suite is the expected `64 B` string allocation when converting an identifier to its textual representation.
