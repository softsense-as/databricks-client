using System.Text.Json.Serialization;

namespace SoftSense.Databricks.SqlClient.Models;

/// <summary>
/// Response from a SQL statement execution
/// </summary>
public sealed class StatementResponse
{
    /// <summary>
    /// Unique statement ID
    /// </summary>
    [JsonPropertyName("statement_id")]
    public string? StatementId { get; set; }

    /// <summary>
    /// Current status of the statement
    /// </summary>
    [JsonPropertyName("status")]
    public StatementStatus? Status { get; set; }

    /// <summary>
    /// Manifest containing result metadata
    /// </summary>
    [JsonPropertyName("manifest")]
    public ResultManifest? Manifest { get; set; }

    /// <summary>
    /// Result data
    /// </summary>
    [JsonPropertyName("result")]
    public ResultData? Result { get; set; }
}

/// <summary>
/// Status of a SQL statement
/// </summary>
public sealed class StatementStatus
{
    /// <summary>
    /// State: PENDING, RUNNING, SUCCEEDED, FAILED, CANCELED, CLOSED
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// Error information if the statement failed
    /// </summary>
    [JsonPropertyName("error")]
    public StatementError? Error { get; set; }
}

/// <summary>
/// Error information for a failed statement
/// </summary>
public sealed class StatementError
{
    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Manifest describing the structure of results
/// </summary>
public sealed class ResultManifest
{
    /// <summary>
    /// Format of the result data
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    /// <summary>
    /// Schema describing column names and types
    /// </summary>
    [JsonPropertyName("schema")]
    public ResultSchema? Schema { get; set; }

    /// <summary>
    /// Total number of rows in result set
    /// </summary>
    [JsonPropertyName("total_row_count")]
    public long? TotalRowCount { get; set; }

    /// <summary>
    /// Total number of chunks
    /// </summary>
    [JsonPropertyName("total_chunk_count")]
    public int? TotalChunkCount { get; set; }

    /// <summary>
    /// Whether result was truncated
    /// </summary>
    [JsonPropertyName("truncated")]
    public bool? Truncated { get; set; }

    /// <summary>
    /// Chunks containing result data
    /// </summary>
    [JsonPropertyName("chunks")]
    public List<ResultChunk>? Chunks { get; set; }
}

/// <summary>
/// Schema describing result columns
/// </summary>
public sealed class ResultSchema
{
    [JsonPropertyName("columns")]
    public List<ColumnInfo>? Columns { get; set; }
}

/// <summary>
/// Information about a result column
/// </summary>
public sealed class ColumnInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type_text")]
    public string? TypeText { get; set; }

    [JsonPropertyName("type_name")]
    public string? TypeName { get; set; }

    [JsonPropertyName("position")]
    public int? Position { get; set; }
}

/// <summary>
/// A chunk of result data
/// </summary>
public sealed class ResultChunk
{
    [JsonPropertyName("chunk_index")]
    public int? ChunkIndex { get; set; }

    [JsonPropertyName("row_offset")]
    public long? RowOffset { get; set; }

    [JsonPropertyName("row_count")]
    public long? RowCount { get; set; }

    [JsonPropertyName("byte_count")]
    public long? ByteCount { get; set; }

    /// <summary>
    /// External link to download chunk data (if using EXTERNAL_LINKS disposition)
    /// </summary>
    [JsonPropertyName("external_links")]
    public List<ExternalLink>? ExternalLinks { get; set; }
}

/// <summary>
/// External link for downloading chunk data
/// </summary>
public sealed class ExternalLink
{
    [JsonPropertyName("external_link")]
    public string? Url { get; set; }

    [JsonPropertyName("expiration")]
    public string? Expiration { get; set; }

    [JsonPropertyName("chunk_index")]
    public int? ChunkIndex { get; set; }

    [JsonPropertyName("row_offset")]
    public long? RowOffset { get; set; }

    [JsonPropertyName("row_count")]
    public long? RowCount { get; set; }
}

/// <summary>
/// Actual result data
/// </summary>
public sealed class ResultData
{
    /// <summary>
    /// Number of rows returned
    /// </summary>
    [JsonPropertyName("row_count")]
    public long? RowCount { get; set; }

    /// <summary>
    /// Row offset for this chunk
    /// </summary>
    [JsonPropertyName("row_offset")]
    public long? RowOffset { get; set; }

    /// <summary>
    /// Chunk index
    /// </summary>
    [JsonPropertyName("chunk_index")]
    public int? ChunkIndex { get; set; }

    /// <summary>
    /// Data values (array of arrays, one per row)
    /// </summary>
    [JsonPropertyName("data_array")]
    public List<List<object?>>? DataArray { get; set; }

    /// <summary>
    /// External links for data (if using EXTERNAL_LINKS disposition)
    /// </summary>
    [JsonPropertyName("external_links")]
    public List<ExternalLink>? ExternalLinks { get; set; }

    /// <summary>
    /// Next chunk index to fetch
    /// </summary>
    [JsonPropertyName("next_chunk_index")]
    public int? NextChunkIndex { get; set; }

    /// <summary>
    /// Internal link for next chunk
    /// </summary>
    [JsonPropertyName("next_chunk_internal_link")]
    public string? NextChunkInternalLink { get; set; }
}
