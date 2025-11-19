using System.Diagnostics;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using SoftSense.Databricks.Core.Configuration;
using SoftSense.Databricks.Core.Exceptions;
using SoftSense.Databricks.SqlClient;

namespace Databricks.Examples.Console;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Display banner
        AnsiConsole.Write(
            new FigletText("Databricks")
                .LeftJustified()
                .Color(Color.Blue));

        AnsiConsole.Write(
            new FigletText("SQL Client Demo")
                .LeftJustified()
                .Color(Color.Green));

        AnsiConsole.WriteLine();

        // Load configuration
        var configuration = new ConfigurationBuilder().BuildStandardConfiguration();
        var config = configuration.GetValidatedSection<DatabricksConfig>();

        // Create client
        using var client = new SqlWarehouseClient(config);

        // Get warehouse ID
        var warehouseId = config.WarehouseId;
        if (string.IsNullOrEmpty(warehouseId))
        {
            warehouseId = AnsiConsole.Ask<string>("Enter [green]SQL Warehouse ID[/]:");
        }

        // Show connection status
        AnsiConsole.MarkupLine($"[green]✓[/] Connected to: [blue]{config.WorkspaceUrl}[/]");
        AnsiConsole.MarkupLine($"[green]✓[/] Authentication: [blue]{GetAuthMethodName(config)}[/]");
        AnsiConsole.WriteLine();

        // Main menu loop
        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]What would you like to do?[/]")
                    .PageSize(10)
                    .AddChoices(new[] {
                        "Quick Query",
                        "Stream Large Dataset",
                        "Explore Tables",
                        "Custom Query",
                        "Configuration Info",
                        "Exit"
                    }));

            AnsiConsole.WriteLine();

            try
            {
                switch (choice)
                {
                    case "Quick Query":
                        await ExecuteQuickQuery(client, warehouseId);
                        break;
                    case "Stream Large Dataset":
                        await StreamDataset(client, warehouseId);
                        break;
                    case "Explore Tables":
                        await ExploreTables(client, warehouseId);
                        break;
                    case "Custom Query":
                        await ExecuteCustomQuery(client, warehouseId);
                        break;
                    case "Configuration Info":
                        ShowConfigurationInfo(config, warehouseId);
                        break;
                    case "Exit":
                        AnsiConsole.MarkupLine("[green]Goodbye![/]");
                        return 0;
                }
            }
            catch (DatabricksAuthenticationException ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Authentication failed:[/] {Markup.Escape(ex.Message)}");
            }
            catch (DatabricksException ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Databricks error:[/] {Markup.Escape(ex.Message)}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Error:[/] {Markup.Escape(ex.Message)}");
            }

            AnsiConsole.WriteLine();
        }
    }

    static string GetAuthMethodName(DatabricksConfig config)
    {
        if (config.Credential == null)
        {
            return "Personal Access Token";
        }

        var credentialType = config.Credential.GetType().Name;
        return credentialType switch
        {
            "InteractiveBrowserCredential" => "Azure Entra ID (Interactive Browser)",
            "DefaultAzureCredential" => "Azure Entra ID (Default Credential)",
            _ => $"Azure Entra ID ({credentialType})"
        };
    }

    static async Task ExecuteQuickQuery(SqlWarehouseClient client, string warehouseId)
    {
        var sql = "SELECT current_timestamp() as timestamp, current_user() as user, current_database() as database LIMIT 10";

        AnsiConsole.MarkupLine($"[dim]Executing:[/] {sql}");
        AnsiConsole.WriteLine();

        var stopwatch = Stopwatch.StartNew();
        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Executing query...", async ctx =>
            {
                return await client.ExecuteQueryAsync(warehouseId, sql);
            });
        stopwatch.Stop();

        DisplayResultsTable(result);

        AnsiConsole.MarkupLine($"[green]✓[/] Retrieved {result.Rows.Count} rows in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }

    static async Task StreamDataset(SqlWarehouseClient client, string warehouseId)
    {
        var limit = AnsiConsole.Ask("How many rows to stream?", 100);
        var sql = $"SELECT * FROM samples.nyctaxi.trips LIMIT {limit}";

        AnsiConsole.MarkupLine($"[dim]Streaming:[/] {sql}");
        AnsiConsole.WriteLine();

        var rowCount = 0;
        var stopwatch = Stopwatch.StartNew();

        await AnsiConsole.Progress()
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
            })
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Streaming rows[/]", maxValue: limit);

                await foreach (var row in client.ExecuteQueryStreamAsync(warehouseId, sql))
                {
                    rowCount++;
                    task.Increment(1);
                }
            });

        stopwatch.Stop();
        AnsiConsole.MarkupLine($"[green]✓[/] Streamed {rowCount} rows in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }

    static async Task ExploreTables(SqlWarehouseClient client, string warehouseId)
    {
        var sql = "SHOW TABLES";

        var stopwatch = Stopwatch.StartNew();
        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Fetching tables...", async ctx =>
            {
                return await client.ExecuteQueryAsync(warehouseId, sql);
            });
        stopwatch.Stop();

        if (result.Rows.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No tables found in current database[/]");
            return;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.Title("[yellow]Available Tables[/]");

        // Add columns
        foreach (var column in result.Columns)
        {
            table.AddColumn(new TableColumn($"[blue]{Markup.Escape(column.Name)}[/]").Centered());
        }

        // Add rows
        foreach (var row in result.Rows.Take(20))
        {
            var values = row.Columns.Select(col => Markup.Escape(row.GetString(col) ?? "NULL")).ToArray();
            table.AddRow(values);
        }

        AnsiConsole.Write(table);

        if (result.Rows.Count > 20)
        {
            AnsiConsole.MarkupLine($"[dim]Showing 20 of {result.Rows.Count} tables[/]");
        }

        AnsiConsole.MarkupLine($"[green]✓[/] Retrieved {result.Rows.Count} rows in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }

    static async Task ExecuteCustomQuery(SqlWarehouseClient client, string warehouseId)
    {
        var sql = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Enter SQL query:[/]")
                .DefaultValue("SELECT 1 as test"));

        AnsiConsole.WriteLine();

        var stopwatch = Stopwatch.StartNew();
        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Executing query...", async ctx =>
            {
                return await client.ExecuteQueryAsync(warehouseId, sql);
            });
        stopwatch.Stop();

        DisplayResultsTable(result);

        AnsiConsole.MarkupLine($"[green]✓[/] Retrieved {result.Rows.Count} rows in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }

    static void DisplayResultsTable(QueryResult result)
    {
        if (result.Columns.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No columns in result[/]");
            return;
        }

        if (result.Rows.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No rows in result[/]");
            return;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);

        // Limit columns to display (max 10 columns to fit in console)
        var maxColumns = 10;
        var columnsToShow = result.Columns.Take(maxColumns).ToList();

        // Add columns
        foreach (var column in columnsToShow)
        {
            table.AddColumn(new TableColumn($"[blue]{Markup.Escape(column.Name)}[/]\n[dim]{Markup.Escape(column.Type)}[/]"));
        }

        // Add rows (limit to 10 for display)
        var rowsToShow = Math.Min(10, result.Rows.Count);
        foreach (var row in result.Rows.Take(rowsToShow))
        {
            var values = columnsToShow.Select(col =>
            {
                var value = row.GetString(col.Name);
                if (string.IsNullOrEmpty(value))
                    return "[dim]NULL[/]";

                // Escape markup and truncate long values
                var escaped = Markup.Escape(value);
                return escaped.Length > 30 ? escaped.Substring(0, 27) + "..." : escaped;
            }).ToArray();
            table.AddRow(values);
        }

        AnsiConsole.Write(table);

        if (result.Columns.Count > maxColumns)
        {
            AnsiConsole.MarkupLine($"[dim]Showing {maxColumns} of {result.Columns.Count} columns[/]");
        }

        if (result.Rows.Count > rowsToShow)
        {
            AnsiConsole.MarkupLine($"[dim]Showing {rowsToShow} of {result.Rows.Count} rows[/]");
        }
    }

    static void ShowConfigurationInfo(DatabricksConfig config, string warehouseId)
    {
        var authInfo = GetAuthMethodName(config);

        // If using PAT, show first 3 chars + masked + length
        if (!string.IsNullOrEmpty(config.AccessToken))
        {
            var token = config.AccessToken;
            var preview = token.Length >= 3 ? token.Substring(0, 3) : token;
            authInfo += $"\n[blue]Access Token:[/] {preview}********* ({token.Length})";
        }

        var panel = new Panel(
            new Markup($"""
            [blue]Workspace URL:[/] {config.WorkspaceUrl}
            [blue]Warehouse ID:[/] {warehouseId}
            [blue]Authentication:[/] {authInfo}
            [blue]Timeout:[/] {config.TimeoutSeconds}s
            [blue]Max Retries:[/] {config.MaxRetries}
            [blue]Polling Interval:[/] {config.PollingIntervalMilliseconds}ms
            """))
        {
            Header = new PanelHeader("[yellow]Configuration[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
    }
}
