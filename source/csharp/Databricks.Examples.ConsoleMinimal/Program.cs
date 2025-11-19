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

try
{
	using var client = new SqlWarehouseClient(databricksConfig);
	var result = await client.ExecuteQueryAsync(sql);

	Console.WriteLine();
	Console.WriteLine($"Rows returned: {result.Rows.Count} (Truncated: {result.Truncated})");

	if (result.Rows.Count == 0)
	{
		Console.WriteLine("Query completed successfully but returned no rows.");
		return;
	}

	var headers = string.Join(" | ", result.Columns.Select(c => c.Name));
	Console.WriteLine(headers);
	Console.WriteLine(new string('-', headers.Length));

	foreach (var row in result.Rows)
	{
		var values = row.Columns.Select(column => row[column]?.ToString() ?? "NULL");
		Console.WriteLine(string.Join(" | ", values));
	}
}
catch (Exception ex)
{
	Console.WriteLine();
	Console.WriteLine("Query failed:");
	Console.WriteLine(ex.Message);
}
