# Databricks SQL Client Benchmarks

> [!WARNING]
> **Work In Progress (WIP)**: The streaming implementation currently retrieves the entire stream before processing, rather than continuously reading and forwarding its position in the stream. Benchmark results may not reflect true streaming performance until this is optimized.

Performance benchmarks comparing different query execution methods.

## Overview

This project uses BenchmarkDotNet to measure and compare the performance of:

- **Batched queries** (`ExecuteQueryAsync`) - Returns full `QueryResult` with all rows in memory
- **Streamed queries** (`ExecuteQueryStreamAsync`) - Returns raw JSON arrays as strings, one at a time
- **JSON queries** (`ExecuteQueryJsonAsync`) - Returns complete JSON array as string
- **NDJSON queries** (`ExecuteQueryStreamNdjsonAsync`) - Returns NDJSON lines with column metadata

## Test Datasets

Benchmarks run against the Databricks sample dataset `samples.nyctaxi.trips`:

- **Small**: 100 rows
- **Medium**: 1,000 rows
- **Large**: 10,000 rows

## Configuration

Set environment variables before running:

```bash
# Windows (CMD)
set DatabricksConfig__WorkspaceUrl=https://your-workspace.azuredatabricks.net
set DatabricksConfig__WarehouseId=your-warehouse-id
set DatabricksConfig__AccessToken=your-token

# Windows (PowerShell)
$env:DatabricksConfig__WorkspaceUrl="https://adb-2672262351800402.2.azuredatabricks.net/"
$env:DatabricksConfig__WarehouseId="8e0c72d725579a2b"
$env:DatabricksConfig__AccessToken="dapib5f9ae638bc97caded8de7bdecd2495f-2"

# Linux/macOS
export DatabricksConfig__WorkspaceUrl=https://your-workspace.azuredatabricks.net
export DatabricksConfig__WarehouseId=your-warehouse-id
export DatabricksConfig__AccessToken=your-token
```

## Running Benchmarks

```bash
cd source/csharp/SoftSense.Databricks.Benchmarks
dotnet run -c Release
```

### Run Specific Benchmarks

```bash
# Run only small dataset benchmarks
dotnet run -c Release --filter *Small*

# Run only streaming benchmarks
dotnet run -c Release --filter *Streamed*

# Run only large dataset benchmarks
dotnet run -c Release --filter *Large*
```

## Interpreting Results

BenchmarkDotNet provides:

- **Mean**: Average execution time
- **Error**: Standard error of the mean
- **StdDev**: Standard deviation
- **Gen0/Gen1/Gen2**: Garbage collection statistics
- **Allocated**: Total memory allocated

### Expected Patterns

- **Batched queries**: Higher memory allocation (all rows in memory), faster for small datasets
- **Streamed queries**: Lower memory allocation (one row at a time), better for large datasets
- **JSON queries**: Minimal parsing overhead, useful for pass-through scenarios
- **NDJSON queries**: Includes column metadata in each line, slightly higher overhead

## Benchmark Results

### Latest Run

> [!NOTE]
> Results will vary based on network latency, warehouse configuration, and dataset size.

```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.7171)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX2


| Method                  | Mean     | Error    | StdDev    | Median   | Gen0      | Gen1      | Gen2      | Allocated   |
|------------------------ |---------:|---------:|----------:|---------:|----------:|----------:|----------:|------------:|
| BatchedQuery_Small      | 498.2 ms |  9.86 ms |  25.46 ms | 496.8 ms |         - |         - |         - |   195.33 KB |
| StreamedQuery_Small     | 491.9 ms |  9.73 ms |  24.94 ms | 487.6 ms |         - |         - |         - |   115.94 KB |
| JsonQuery_Small         | 583.0 ms | 26.43 ms |  77.52 ms | 614.0 ms |         - |         - |         - |   336.42 KB |
| BatchedQuery_Medium     | 646.1 ms | 26.06 ms |  74.35 ms | 632.5 ms |         - |         - |         - |  1819.19 KB |
| StreamedQuery_Medium    | 664.7 ms | 26.98 ms |  78.27 ms | 656.3 ms |         - |         - |         - |   814.39 KB |
| JsonQuery_Medium        | 624.5 ms | 15.40 ms |  44.42 ms | 628.2 ms |         - |         - |         - |  3518.33 KB |
| BatchedQuery_Large      | 788.8 ms | 39.02 ms | 110.69 ms | 782.7 ms | 2000.0000 | 1000.0000 |         - | 18153.79 KB |
| StreamedQuery_Large     | 812.3 ms | 38.44 ms | 112.15 ms | 785.9 ms | 1000.0000 | 1000.0000 | 1000.0000 |  7994.34 KB |
| JsonQuery_Large         | 775.7 ms | 23.90 ms |  69.34 ms | 777.4 ms | 2000.0000 | 1000.0000 |         - | 36092.73 KB |
| NdjsonStreamQuery_Large | 762.5 ms | 25.92 ms |  74.78 ms | 774.2 ms | 6000.0000 | 2000.0000 | 1000.0000 | 39453.81 KB |

```

### Analysis

<!-- Add your analysis of the results here -->

## Notes

- Benchmarks require a live Databricks workspace connection
- First run may be slower due to warehouse startup time
- Results vary based on network latency and warehouse performance
- Memory diagnostics show allocation patterns (streaming should allocate less)
- BenchmarkDotNet generates markdown tables in `BenchmarkDotNet.Artifacts/results/` directory
