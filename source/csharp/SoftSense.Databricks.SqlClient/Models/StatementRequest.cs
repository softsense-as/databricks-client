using System.Text.Json.Serialization;

namespace SoftSense.Databricks.SqlClient.Models;

/// <summary>
/// Request to execute a SQL statement on a warehouse
/// </summary>
public sealed class StatementRequest
{
    /// <summary>
    /// SQL Warehouse ID to execute the query on
    /// </summary>
    [JsonPropertyName("warehouse_id")]
    public required string WarehouseId { get; init; }

    /// <summary>
    /// SQL statement to execute
    /// </summary>
    [JsonPropertyName("statement")]
    public required string Statement { get; init; }

    /// <summary>
    /// Catalog to use (optional)
    /// </summary>
    [JsonPropertyName("catalog")]
    public string? Catalog { get; init; }

    /// <summary>
    /// Schema to use (optional)
    /// </summary>
    [JsonPropertyName("schema")]
    public string? Schema { get; init; }

    /// <summary>
    /// Parameters for the query (optional)
    /// </summary>
    [JsonPropertyName("parameters")]
    public List<StatementParameter>? Parameters { get; init; }

    /// <summary>
    /// Maximum number of rows to return (optional, default: 100000)
    /// </summary>
    [JsonPropertyName("row_limit")]
    public int? RowLimit { get; init; }

    /// <summary>
    /// Number of bytes per fetch chunk (optional)
    /// </summary>
    [JsonPropertyName("byte_limit")]
    public int? ByteLimit { get; init; }

    /// <summary>
    /// Disposition mode: INLINE or EXTERNAL_LINKS (default: INLINE)
    /// </summary>
    [JsonPropertyName("disposition")]
    public string? Disposition { get; init; }

    /// <summary>
    /// Format for returned data: JSON_ARRAY, ARROW_STREAM, or CSV (default: JSON_ARRAY)
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; init; }

    /// <summary>
    /// Timeout for query execution in seconds (optional)
    /// </summary>
    [JsonPropertyName("wait_timeout")]
    public string? WaitTimeout { get; init; }

    /// <summary>
    /// Whether to return results on error (optional)
    /// </summary>
    [JsonPropertyName("on_wait_timeout")]
    public string? OnWaitTimeout { get; init; }
}

/// <summary>
/// Parameter for a SQL statement
/// </summary>
public sealed class StatementParameter
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }
}
