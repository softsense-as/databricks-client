using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using SoftSense.Databricks.Core.Configuration;
using SoftSense.Databricks.SqlClient;

var configuration = new ConfigurationBuilder().BuildStandardConfiguration();
var databricksConfig = configuration.GetValidatedSection<DatabricksConfig>();

Console.WriteLine("Databricks Minimal Console Example");
Console.WriteLine();
Console.Write("Enter SQL query: ");

var sql = Console.ReadLine();
if (string.IsNullOrWhiteSpace(sql))
{
	Console.WriteLine("No SQL query provided. Exiting.");
	return;
}

Console.WriteLine();
Console.WriteLine("Select fetch method:");
Console.WriteLine("1. Full Result as QueryResult (ExecuteQueryAsync)");
Console.WriteLine("2. Full Result as JSON Objects (ExecuteQueryJsonAsync)");
Console.WriteLine("3. Stream JSON Rows (ExecuteQueryStreamAsync)");
Console.WriteLine("4. Stream NDJSON (ExecuteQueryStreamNdjsonAsync)");
Console.WriteLine("5. Sample JSON (LIMIT 1)");
Console.WriteLine("6. JSON Schema");
Console.Write("Choice (1-6): ");

var choice = Console.ReadLine();

Console.WriteLine();
Console.Write("Print records to console? (y/n): ");
var printRecordsInput = Console.ReadLine()?.Trim().ToLowerInvariant();
var printRecords = printRecordsInput == "y" || printRecordsInput == "yes";

var sw = Stopwatch.StartNew();
var rowCount = 0;
var status = "Unknown";

try
{
	using var client = new SqlWarehouseClient(databricksConfig);

	switch (choice)
	{
		case "1":
			rowCount = await ExecuteFullResultAsync(client, sql, printRecords);
			status = "Success";
			break;
		case "2":
			rowCount = await ExecuteFullResultJsonAsync(client, sql, printRecords);
			status = "Success";
			break;
		case "3":
			rowCount = await ExecuteStreamAsync(client, sql, printRecords);
			status = "Success";
			break;
		case "4":
			rowCount = await ExecuteStreamNdjsonAsync(client, sql, printRecords);
			status = "Success";
			break;
		case "5":
			rowCount = await ExecuteSampleJsonAsync(client, sql, printRecords);
			status = "Success";
			break;
		case "6":
			rowCount = await ExecuteJsonSchemaAsync(client, sql, printRecords);
			status = "Success";
			break;
		default:
			Console.WriteLine("Invalid choice. Exiting.");
			status = "Invalid Choice";
			break;
	}
}
catch (Exception ex)
{
	Console.WriteLine();
	Console.WriteLine("Query failed:");
	Console.WriteLine(ex.Message);
	status = "Failed";
}
finally
{
	sw.Stop();
	Console.WriteLine();
	Console.WriteLine("=== Execution Summary ===");
	Console.WriteLine($"Status: {status}");
	Console.WriteLine($"Records: {rowCount}");
	Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
}

static async Task<int> ExecuteFullResultAsync(SqlWarehouseClient client, string sql, bool printRecords)
{
	var result = await client.ExecuteQueryAsync(sql);

	if (printRecords)
	{
		Console.WriteLine();
		Console.WriteLine("Full Result as QueryResult (raw arrays):");
		Console.WriteLine();

		// Convert all rows to JSON array
		var jsonArray = result.Rows.Select(row => row.Values).ToList();
		var json = System.Text.Json.JsonSerializer.Serialize(jsonArray, new System.Text.Json.JsonSerializerOptions
		{
			WriteIndented = true
		});

		Console.WriteLine(json);
	}

	return result.Rows.Count;
}

static async Task<int> ExecuteFullResultJsonAsync(SqlWarehouseClient client, string sql, bool printRecords)
{
	var json = await client.ExecuteQueryJsonAsync(sql);

	if (printRecords)
	{
		Console.WriteLine();
		Console.WriteLine("Full Result as JSON Objects:");
		Console.WriteLine();
		Console.WriteLine(json);
	}

	// Count rows by parsing the JSON array length
	using var doc = System.Text.Json.JsonDocument.Parse(json);
	return doc.RootElement.GetArrayLength();
}

static async Task<int> ExecuteStreamAsync(SqlWarehouseClient client, string sql, bool printRecords)
{
	var rowCount = 0;

	if (printRecords)
	{
		Console.WriteLine();
		Console.WriteLine("Streaming JSON Rows:");
		Console.WriteLine();
	}

	await foreach (var jsonRow in client.ExecuteQueryStreamAsync(sql))
	{
		rowCount++;
		if (printRecords)
		{
			Console.WriteLine(jsonRow);
		}
	}

	return rowCount;
}

static async Task<int> ExecuteStreamNdjsonAsync(SqlWarehouseClient client, string sql, bool printRecords)
{
	var rowCount = 0;

	if (printRecords)
	{
		Console.WriteLine();
		Console.WriteLine("Streaming NDJSON (with column names):");
		Console.WriteLine();
	}

	await foreach (var ndjsonRow in client.ExecuteQueryStreamNdjsonAsync(sql))
	{
		rowCount++;
		if (printRecords)
		{
			Console.WriteLine(ndjsonRow);
		}
	}

	return rowCount;
}

static async Task<int> ExecuteSampleJsonAsync(SqlWarehouseClient client, string sql, bool printRecords)
{
	// Strip any existing LIMIT clause and add LIMIT 1
	var limitedSql = StripLimitClause(sql) + " LIMIT 1";

	var result = await client.ExecuteQueryAsync(limitedSql);

	Console.WriteLine();
	Console.WriteLine("Sample JSON (LIMIT 1):");
	Console.WriteLine();

	if (result.Rows.Count == 0)
	{
		Console.WriteLine("{}");
		return 0;
	}

	// Create a JSON object with column names as keys
	var sampleObject = new Dictionary<string, object?>();
	var row = result.Rows[0];

	for (int i = 0; i < result.Columns.Count; i++)
	{
		sampleObject[result.Columns[i].Name] = row.Values[i];
	}

	var json = System.Text.Json.JsonSerializer.Serialize(sampleObject, new System.Text.Json.JsonSerializerOptions
	{
		WriteIndented = true
	});

	Console.WriteLine(json);

	return result.Rows.Count > 0 ? 1 : 0;
}

static async Task<int> ExecuteJsonSchemaAsync(SqlWarehouseClient client, string sql, bool printRecords)
{
	// Strip any existing LIMIT clause and add LIMIT 1 to get column metadata
	var limitedSql = StripLimitClause(sql) + " LIMIT 1";

	var result = await client.ExecuteQueryAsync(limitedSql);

	Console.WriteLine();
	Console.WriteLine("JSON Schema:");
	Console.WriteLine();

	// Build JSON Schema
	var schema = new Dictionary<string, object>
	{
		["$schema"] = "http://json-schema.org/draft-07/schema#",
		["type"] = "object",
		["properties"] = new Dictionary<string, object>()
	};

	var properties = (Dictionary<string, object>)schema["properties"];
	var required = new List<string>();

	foreach (var column in result.Columns)
	{
		var columnSchema = new Dictionary<string, object>
		{
			["description"] = $"{column.Name} ({column.Type})"
		};

		// Map Databricks types to JSON Schema types
		var jsonType = MapDatabricksTypeToJsonType(column.Type);
		columnSchema["type"] = jsonType;

		properties[column.Name] = columnSchema;
		required.Add(column.Name);
	}

	schema["required"] = required;

	var json = System.Text.Json.JsonSerializer.Serialize(schema, new System.Text.Json.JsonSerializerOptions
	{
		WriteIndented = true
	});

	Console.WriteLine(json);

	return result.Columns.Count;
}

static string StripLimitClause(string sql)
{
	// Remove trailing semicolon and whitespace
	sql = sql.TrimEnd().TrimEnd(';').TrimEnd();

	// Strip LIMIT clause using regex (case-insensitive)
	// Matches: LIMIT <number> or LIMIT <number> OFFSET <number>
	var pattern = @"\s+LIMIT\s+\d+(\s+OFFSET\s+\d+)?$";
	sql = Regex.Replace(sql, pattern, "", RegexOptions.IgnoreCase);

	return sql;
}

static string MapDatabricksTypeToJsonType(string databricksType)
{
	var type = databricksType.ToLowerInvariant();

	if (type.Contains("int") || type.Contains("long") || type.Contains("short") || type.Contains("byte"))
		return "integer";

	if (type.Contains("float") || type.Contains("double") || type.Contains("decimal"))
		return "number";

	if (type.Contains("bool"))
		return "boolean";

	if (type.Contains("array"))
		return "array";

	if (type.Contains("struct") || type.Contains("map"))
		return "object";

	// Default to string for varchar, string, date, timestamp, etc.
	return "string";
}
