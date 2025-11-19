using BenchmarkDotNet.Running;

namespace SoftSense.Databricks.Benchmarks;

internal static class Program
{
    private static void Main(string[] args)
    {
        BenchmarkRunner.Run<QueryBenchmarks>(args: args);
    }
}
