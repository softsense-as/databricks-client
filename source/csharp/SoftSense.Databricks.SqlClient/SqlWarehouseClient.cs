using SoftSense.Databricks.Core.Configuration;
using SoftSense.Databricks.Core.Http;
using SoftSense.Databricks.SqlClient.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace SoftSense.Databricks.SqlClient;

/// <summary>
/// Client for executing SQL queries on Databricks SQL Warehouses
/// </summary>
public sealed class SqlWarehouseClient : IDisposable
{
    private readonly DatabricksHttpClient _httpClient;
    private readonly DatabricksConfig _config;
    private const string StatementsEndpoint = "/api/2.0/sql/statements";

    public SqlWarehouseClient(DatabricksConfig config)
    {
        _config = config;
        _httpClient = new DatabricksHttpClient(config);
    }

    /// <summary>
    /// Executes a SQL query using the default warehouse, catalog, and schema from configuration
    /// </summary>
    /// <param name="sql">SQL query to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Query result with all rows</returns>
    /// <exception cref="InvalidOperationException">Thrown when WarehouseId is not configured</exception>
    public Task<QueryResult> ExecuteQueryAsync(
        string sql,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_config.WarehouseId))
        {
            throw new InvalidOperationException(
                "WarehouseId must be configured in DatabricksConfig to use this overload. " +
                "Use ExecuteQueryAsync(warehouseId, sql, ...) to specify warehouse explicitly.");
        }

        return ExecuteQueryAsync(
            _config.WarehouseId,
            sql,
            cancellationToken);
    }

    /// <summary>
    /// Executes a SQL query and returns the complete result as JSON string (array of objects with column names)
    /// </summary>
    /// <param name="sql">SQL query to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON string containing array of objects</returns>
    /// <exception cref="InvalidOperationException">Thrown when WarehouseId is not configured</exception>
    public Task<string> ExecuteQueryJsonAsync(
        string sql,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_config.WarehouseId))
        {
            throw new InvalidOperationException(
                "WarehouseId must be configured in DatabricksConfig to use this overload. " +
                "Use ExecuteQueryJsonAsync(warehouseId, sql, ...) to specify warehouse explicitly.");
        }

        return ExecuteQueryJsonAsync(
            _config.WarehouseId,
            sql,
            cancellationToken);
    }

    /// <summary>
    /// Executes a SQL query and returns the complete result
    /// </summary>
    /// <param name="warehouseId">SQL Warehouse ID</param>
    /// <param name="sql">SQL query to execute</param>
    /// <param name="catalog">Optional catalog name</param>
    /// <param name="schema">Optional schema name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Query result with all rows</returns>
    public async Task<QueryResult> ExecuteQueryAsync(
        string warehouseId,
        string sql,
        CancellationToken cancellationToken = default)
    {
        var request = new StatementRequest
        {
            WarehouseId = warehouseId,
            Statement = sql,
            Disposition = "INLINE",
            Format = "JSON_ARRAY",
            WaitTimeout = "30s"
        };

        var response = await _httpClient.PostAsync<StatementRequest, StatementResponse>(
            StatementsEndpoint,
            request,
            cancellationToken);

        if (response.Status?.State == "FAILED")
        {
            throw new InvalidOperationException(
                $"Query failed: {response.Status.Error?.Message ?? "Unknown error"}");
        }

        // Wait for query to complete if still running
        while (response.Status?.State is "PENDING" or "RUNNING")
        {
            await Task.Delay(_config.PollingIntervalMilliseconds, cancellationToken);
            response = await GetStatementAsync(response.StatementId!, cancellationToken);
        }

        return ConvertToQueryResult(response);
    }

    /// <summary>
    /// Executes a SQL query and returns the complete result as JSON string (array of objects with column names)
    /// </summary>
    /// <param name="warehouseId">SQL Warehouse ID</param>
    /// <param name="sql">SQL query to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON string containing array of objects</returns>
    public async Task<string> ExecuteQueryJsonAsync(
        string warehouseId,
        string sql,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteQueryAsync(warehouseId, sql, cancellationToken);

        // Convert result to JSON array of objects
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartArray();

            foreach (var row in result.Rows)
            {
                writer.WriteStartObject();
                for (int i = 0; i < result.Columns.Count; i++)
                {
                    writer.WritePropertyName(result.Columns[i].Name);

                    var value = row.Values[i];
                    WriteJsonValue(writer, value);
                }
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Executes a SQL query and streams raw JSON result strings using the default warehouse
    /// </summary>
    /// <param name="sql">SQL query to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of raw JSON strings (one per row)</returns>
    /// <exception cref="InvalidOperationException">Thrown when WarehouseId is not configured</exception>
    public IAsyncEnumerable<string> ExecuteQueryStreamAsync(
        string sql,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_config.WarehouseId))
        {
            throw new InvalidOperationException(
                "WarehouseId must be configured in DatabricksConfig to use this overload. " +
                "Use ExecuteQueryStreamAsync(warehouseId, sql, ...) to specify warehouse explicitly.");
        }

        return ExecuteQueryStreamAsync(
            _config.WarehouseId,
            sql,
            cancellationToken);
    }

    /// <summary>
    /// Executes a SQL query and streams NDJSON (newline-delimited JSON) with column names using the default warehouse
    /// </summary>
    /// <param name="sql">SQL query to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of NDJSON strings (JSON objects with column names)</returns>
    /// <exception cref="InvalidOperationException">Thrown when WarehouseId is not configured</exception>
    public IAsyncEnumerable<string> ExecuteQueryStreamNdjsonAsync(
        string sql,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_config.WarehouseId))
        {
            throw new InvalidOperationException(
                "WarehouseId must be configured in DatabricksConfig to use this overload. " +
                "Use ExecuteQueryStreamNdjsonAsync(warehouseId, sql, ...) to specify warehouse explicitly.");
        }

        return ExecuteQueryStreamNdjsonAsync(
            _config.WarehouseId,
            sql,
            cancellationToken);
    }

    /// <summary>
    /// Executes a SQL query and streams raw JSON result strings
    /// </summary>
    /// <param name="warehouseId">SQL Warehouse ID</param>
    /// <param name="sql">SQL query to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of raw JSON strings (one per row)</returns>
    public async IAsyncEnumerable<string> ExecuteQueryStreamAsync(
        string warehouseId,
        string sql,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new StatementRequest
        {
            WarehouseId = warehouseId,
            Statement = sql,
            Disposition = "INLINE",
            Format = "JSON_ARRAY",
            WaitTimeout = "30s"
        };

        var stream = await _httpClient.PostStreamAsync(StatementsEndpoint, request, cancellationToken);
        using var streamReader = new StreamReader(stream);
        var responseJson = await streamReader.ReadToEndAsync(cancellationToken);

        var (statementId, state, errorMessage) = ParseStatusResponse(responseJson);

        if (state == "FAILED")
        {
            throw new InvalidOperationException($"Query failed: {errorMessage}");
        }

        while (state is "PENDING" or "RUNNING")
        {
            await Task.Delay(_config.PollingIntervalMilliseconds, cancellationToken);

            var pollStream = await _httpClient.GetStreamAsync($"{StatementsEndpoint}/{statementId}", cancellationToken);
            using var pollReader = new StreamReader(pollStream);
            responseJson = await pollReader.ReadToEndAsync(cancellationToken);

            (_, state, errorMessage) = ParseStatusResponse(responseJson);

            if (state == "FAILED")
            {
                throw new InvalidOperationException($"Query failed: {errorMessage}");
            }
        }

        foreach (var row in ExtractDataArrayRows(responseJson))
        {
            yield return row;
        }

        var nextChunkIndex = ExtractNextChunkIndex(responseJson);
        while (nextChunkIndex.HasValue && statementId is not null)
        {
            var chunkStream = await _httpClient.GetStreamAsync(
                $"{StatementsEndpoint}/{statementId}/result/chunks/{nextChunkIndex.Value}",
                cancellationToken);

            using var chunkReader = new StreamReader(chunkStream);
            var chunkJson = await chunkReader.ReadToEndAsync(cancellationToken);

            foreach (var row in ExtractDataArrayRows(chunkJson))
            {
                yield return row;
            }

            nextChunkIndex = ExtractNextChunkIndex(chunkJson);
        }
    }

    /// <summary>
    /// Executes a SQL query and streams NDJSON (newline-delimited JSON) with column names
    /// </summary>
    /// <param name="warehouseId">SQL Warehouse ID</param>
    /// <param name="sql">SQL query to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of NDJSON strings (JSON objects with column names)</returns>
    public async IAsyncEnumerable<string> ExecuteQueryStreamNdjsonAsync(
        string warehouseId,
        string sql,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new StatementRequest
        {
            WarehouseId = warehouseId,
            Statement = sql,
            Disposition = "INLINE",
            Format = "JSON_ARRAY",
            WaitTimeout = "30s"
        };

        var stream = await _httpClient.PostStreamAsync(StatementsEndpoint, request, cancellationToken);
        using var streamReader = new StreamReader(stream);
        var responseJson = await streamReader.ReadToEndAsync(cancellationToken);

        var (statementId, state, errorMessage) = ParseStatusResponse(responseJson);

        if (state == "FAILED")
        {
            throw new InvalidOperationException($"Query failed: {errorMessage}");
        }

        while (state is "PENDING" or "RUNNING")
        {
            await Task.Delay(_config.PollingIntervalMilliseconds, cancellationToken);

            var pollStream = await _httpClient.GetStreamAsync($"{StatementsEndpoint}/{statementId}", cancellationToken);
            using var pollReader = new StreamReader(pollStream);
            responseJson = await pollReader.ReadToEndAsync(cancellationToken);

            (_, state, errorMessage) = ParseStatusResponse(responseJson);

            if (state == "FAILED")
            {
                throw new InvalidOperationException($"Query failed: {errorMessage}");
            }
        }

        // Extract column names from response
        var columnNames = ExtractColumnNames(responseJson);

        // Stream rows as NDJSON (JSON objects with column names)
        foreach (var rowArrayJson in ExtractDataArrayRows(responseJson))
        {
            var ndjsonRow = ConvertRowToNdjson(rowArrayJson, columnNames);
            yield return ndjsonRow;
        }

        var nextChunkIndex = ExtractNextChunkIndex(responseJson);
        while (nextChunkIndex.HasValue && statementId is not null)
        {
            var chunkStream = await _httpClient.GetStreamAsync(
                $"{StatementsEndpoint}/{statementId}/result/chunks/{nextChunkIndex.Value}",
                cancellationToken);

            using var chunkReader = new StreamReader(chunkStream);
            var chunkJson = await chunkReader.ReadToEndAsync(cancellationToken);

            foreach (var rowArrayJson in ExtractDataArrayRows(chunkJson))
            {
                var ndjsonRow = ConvertRowToNdjson(rowArrayJson, columnNames);
                yield return ndjsonRow;
            }

            nextChunkIndex = ExtractNextChunkIndex(chunkJson);
        }
    }

    private static (string? statementId, string? state, string errorMessage) ParseStatusResponse(string json)
    {
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        string? statementId = null;
        string? state = null;
        string errorMessage = "Unknown error";

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                if (propertyName == "statement_id")
                {
                    statementId = reader.GetString();
                }
                else if (propertyName == "status")
                {
                    (state, errorMessage) = ParseStatusObject(ref reader);
                }
            }
        }

        return (statementId, state, errorMessage);
    }

    private static (string? state, string errorMessage) ParseStatusObject(ref Utf8JsonReader reader)
    {
        string? state = null;
        string errorMessage = "Unknown error";

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    if (propertyName == "state")
                    {
                        state = reader.GetString();
                    }
                    else if (propertyName == "error")
                    {
                        errorMessage = ParseErrorObject(ref reader);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
            }
        }

        return (state, errorMessage);
    }

    private static string ParseErrorObject(ref Utf8JsonReader reader)
    {
        string errorMessage = "Unknown error";

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    if (propertyName == "message")
                    {
                        errorMessage = reader.GetString() ?? errorMessage;
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
            }
        }

        return errorMessage;
    }

    private static IEnumerable<string> ExtractDataArrayRows(string json)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(bytes);

        var inResult = false;
        var rows = new List<string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                if (propertyName == "result")
                {
                    inResult = true;
                }
                else if (inResult && propertyName == "data_array")
                {
                    reader.Read(); // Move to StartArray

                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                var startPosition = reader.TokenStartIndex;
                                reader.Skip(); // Skip the entire array to get to end
                                var endPosition = reader.TokenStartIndex + 1;

                                var rowJson = System.Text.Encoding.UTF8.GetString(
                                    bytes.AsSpan((int)startPosition, (int)(endPosition - startPosition)));
                                rows.Add(rowJson);
                            }
                        }
                    }
                    break;
                }
            }
        }

        return rows;
    }

    private static int? ExtractNextChunkIndex(string json)
    {
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        var inResult = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();

                if (propertyName == "result")
                {
                    inResult = true;
                }
                else if (inResult && propertyName == "next_chunk_index")
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.Number)
                    {
                        return reader.GetInt32();
                    }
                }
            }
        }

        return null;
    }

    private static List<string> ExtractColumnNames(string json)
    {
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        var columnNames = new List<string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "manifest")
            {
                // Navigate to schema -> columns
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "schema")
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                        {
                            if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "columns")
                            {
                                reader.Read(); // Move to StartArray
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                                    {
                                        if (reader.TokenType == JsonTokenType.StartObject)
                                        {
                                            // Read column object to find name
                                            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                                            {
                                                if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "name")
                                                {
                                                    reader.Read();
                                                    columnNames.Add(reader.GetString() ?? "");
                                                }
                                            }
                                        }
                                    }
                                }
                                return columnNames;
                            }
                        }
                    }
                }
            }
        }

        return columnNames;
    }

    private static string ConvertRowToNdjson(string rowArrayJson, List<string> columnNames)
    {
        // Parse the row array JSON
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(rowArrayJson));
        var values = new List<JsonElement>();

        if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                values.Add(doc.RootElement.Clone());
            }
        }

        // Build JSON object with column names as keys
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            for (int i = 0; i < Math.Min(columnNames.Count, values.Count); i++)
            {
                writer.WritePropertyName(columnNames[i]);
                values[i].WriteTo(writer);
            }
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Gets the status and result of a statement
    /// </summary>
    private async Task<StatementResponse> GetStatementAsync(
        string statementId,
        CancellationToken cancellationToken)
    {
        return await _httpClient.GetAsync<StatementResponse>(
            $"{StatementsEndpoint}/{statementId}",
            cancellationToken);
    }

    /// <summary>
    /// Gets a specific chunk of results
    /// </summary>
    private async Task<StatementResponse> GetStatementChunkAsync(
        string statementId,
        int chunkIndex,
        CancellationToken cancellationToken)
    {
        return await _httpClient.GetAsync<StatementResponse>(
            $"{StatementsEndpoint}/{statementId}/result/chunks/{chunkIndex}",
            cancellationToken);
    }

    /// <summary>
    /// Converts API response to QueryResult
    /// </summary>
    private static QueryResult ConvertToQueryResult(StatementResponse response)
    {
        var columns = response.Manifest?.Schema?.Columns?
            .Select(c => new QueryColumn(c.Name ?? string.Empty, c.TypeText ?? "string"))
            .ToList() ?? [];

        var rows = response.Result?.DataArray?
            .Select(row => new QueryRow(
                columns.Select(c => c.Name).ToList(),
                row))
            .ToList() ?? [];

        return new QueryResult
        {
            Columns = columns,
            Rows = rows,
            TotalRowCount = response.Manifest?.TotalRowCount ?? rows.Count,
            Truncated = response.Manifest?.Truncated ?? false
        };
    }

    /// <summary>
    /// Writes a value to a Utf8JsonWriter without using reflection-based serialization
    /// </summary>
    private static void WriteJsonValue(Utf8JsonWriter writer, object? value)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else if (value is string stringValue)
        {
            writer.WriteStringValue(stringValue);
        }
        else if (value is bool boolValue)
        {
            writer.WriteBooleanValue(boolValue);
        }
        else if (value is int intValue)
        {
            writer.WriteNumberValue(intValue);
        }
        else if (value is long longValue)
        {
            writer.WriteNumberValue(longValue);
        }
        else if (value is float floatValue)
        {
            writer.WriteNumberValue(floatValue);
        }
        else if (value is double doubleValue)
        {
            writer.WriteNumberValue(doubleValue);
        }
        else if (value is decimal decimalValue)
        {
            writer.WriteNumberValue(decimalValue);
        }
        else if (value is JsonElement jsonElement)
        {
            jsonElement.WriteTo(writer);
        }
        else
        {
            // Fallback for other types - convert to string
            writer.WriteStringValue(value.ToString());
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

/// <summary>
/// Result of a SQL query execution
/// </summary>
public sealed class QueryResult
{
    public required List<QueryColumn> Columns { get; init; }
    public required List<QueryRow> Rows { get; init; }
    public long TotalRowCount { get; init; }
    public bool Truncated { get; init; }
}

/// <summary>
/// Column metadata
/// </summary>
public sealed record QueryColumn(string Name, string Type);

/// <summary>
/// A row of query results
/// </summary>
public sealed class QueryRow
{
    private readonly List<string> _columns;
    private readonly List<object?> _values;

    public QueryRow(List<string> columns, List<object?> values)
    {
        _columns = columns;
        _values = values;
    }

    /// <summary>
    /// Get value by column index
    /// </summary>
    public object? this[int index] => _values[index];

    /// <summary>
    /// Get value by column name
    /// </summary>
    public object? this[string columnName]
    {
        get
        {
            var index = _columns.IndexOf(columnName);
            return index >= 0 ? _values[index] : null;
        }
    }

    /// <summary>
    /// Get value as string
    /// </summary>
    public string? GetString(string columnName) => this[columnName]?.ToString();

    /// <summary>
    /// Get value as int
    /// </summary>
    public int? GetInt(string columnName)
    {
        var value = this[columnName];
        return value switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var i) => i,
            _ => null
        };
    }

    /// <summary>
    /// Get value as long
    /// </summary>
    public long? GetLong(string columnName)
    {
        var value = this[columnName];
        return value switch
        {
            long l => l,
            int i => i,
            string s when long.TryParse(s, out var l) => l,
            _ => null
        };
    }

    /// <summary>
    /// Get value as double
    /// </summary>
    public double? GetDouble(string columnName)
    {
        var value = this[columnName];
        return value switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            string s when double.TryParse(s, out var d) => d,
            _ => null
        };
    }

    /// <summary>
    /// Get value as bool
    /// </summary>
    public bool? GetBool(string columnName)
    {
        var value = this[columnName];
        return value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var b) => b,
            _ => null
        };
    }

    /// <summary>
    /// Get all values as dictionary
    /// </summary>
    public Dictionary<string, object?> ToDictionary()
    {
        return _columns.Zip(_values, (k, v) => new { k, v })
            .ToDictionary(x => x.k, x => x.v);
    }

    /// <summary>
    /// Get all column names
    /// </summary>
    public IReadOnlyList<string> Columns => _columns;

    /// <summary>
    /// Get all values
    /// </summary>
    public IReadOnlyList<object?> Values => _values;
}
