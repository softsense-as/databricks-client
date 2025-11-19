using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Configuration;
using SoftSense.Databricks.Core.Configuration;
using SoftSense.Databricks.SqlClient;

namespace SoftSense.Databricks.Benchmarks;

/// <summary>
/// Benchmarks comparing batched vs streamed query execution
/// </summary>
[SimpleJob]
[MemoryDiagnoser]
[MarkdownExporter]
public class QueryBenchmarks
{
    private SqlWarehouseClient? _client;
    private string? _warehouseId;
    private const string SmallQuerySql = "SELECT * FROM samples.nyctaxi.trips LIMIT 100";
    private const string MediumQuerySql = "SELECT * FROM samples.nyctaxi.trips LIMIT 1000";
    private const string LargeQuerySql = "SELECT * FROM samples.nyctaxi.trips LIMIT 10000";

    [GlobalSetup]
    public void Setup()
    {
        var configuration = new ConfigurationBuilder().BuildStandardConfiguration();
        var config = configuration.GetValidatedSection<DatabricksConfig>();

        _warehouseId = config.WarehouseId
            ?? throw new InvalidOperationException("WarehouseId must be configured for benchmarks");

        _client = new SqlWarehouseClient(config);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
    }

    // Small dataset benchmarks (100 rows)

    [Benchmark]
    public async Task<int> BatchedQuery_Small()
    {
        var result = await _client!.ExecuteQueryAsync(_warehouseId!, SmallQuerySql);
        return result.Rows.Count;
    }

    [Benchmark]
    public async Task<int> StreamedQuery_Small()
    {
        var count = 0;
        await foreach (var _ in _client!.ExecuteQueryStreamAsync(_warehouseId!, SmallQuerySql))
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public async Task<int> JsonQuery_Small()
    {
        var json = await _client!.ExecuteQueryJsonAsync(_warehouseId!, SmallQuerySql);
        return json.Length;
    }

    // Medium dataset benchmarks (1000 rows)

    [Benchmark]
    public async Task<int> BatchedQuery_Medium()
    {
        var result = await _client!.ExecuteQueryAsync(_warehouseId!, MediumQuerySql);
        return result.Rows.Count;
    }

    [Benchmark]
    public async Task<int> StreamedQuery_Medium()
    {
        var count = 0;
        await foreach (var _ in _client!.ExecuteQueryStreamAsync(_warehouseId!, MediumQuerySql))
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public async Task<int> JsonQuery_Medium()
    {
        var json = await _client!.ExecuteQueryJsonAsync(_warehouseId!, MediumQuerySql);
        return json.Length;
    }

    // Large dataset benchmarks (10000 rows)

    [Benchmark]
    public async Task<int> BatchedQuery_Large()
    {
        var result = await _client!.ExecuteQueryAsync(_warehouseId!, LargeQuerySql);
        return result.Rows.Count;
    }

    [Benchmark]
    public async Task<int> StreamedQuery_Large()
    {
        var count = 0;
        await foreach (var _ in _client!.ExecuteQueryStreamAsync(_warehouseId!, LargeQuerySql))
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public async Task<int> JsonQuery_Large()
    {
        var json = await _client!.ExecuteQueryJsonAsync(_warehouseId!, LargeQuerySql);
        return json.Length;
    }

    // NDJSON streaming benchmark

    [Benchmark]
    public async Task<int> NdjsonStreamQuery_Large()
    {
        var count = 0;
        await foreach (var _ in _client!.ExecuteQueryStreamNdjsonAsync(_warehouseId!, LargeQuerySql))
        {
            count++;
        }
        return count;
    }
}
