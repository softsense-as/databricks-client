using System.Diagnostics;
using Azure.Identity;
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

        // Get configuration
        var config = GetConfiguration();
        if (config == null)
        {
            AnsiConsole.MarkupLine("[red]✗[/] Configuration failed. Please set environment variables.");
            return 1;
        }

        // Create client
        using var client = new SqlWarehouseClient(config);

        // Get warehouse ID
        var warehouseId = Environment.GetEnvironmentVariable("DATABRICKS_WAREHOUSE_ID");
        if (string.IsNullOrEmpty(warehouseId))
        {
            warehouseId = AnsiConsole.Ask<string>("Enter [green]SQL Warehouse ID[/]:");
        }

        // Show connection status
        AnsiConsole.MarkupLine($"[green]✓[/] Connected to: [blue]{config.WorkspaceUrl}[/]");
        AnsiConsole.MarkupLine($"[green]✓[/] Authentication: [blue]{GetAuthMethodName(config)}[/]");
        AnsiConsole.WriteLine();

        // Check if running in demo mode (non-interactive, like in Aspire)
        var isDemoMode = !AnsiConsole.Profile.Capabilities.Interactive ||
                         Environment.GetEnvironmentVariable("DEMO_MODE") == "true";

        if (isDemoMode)
        {
            // Run automated demo
            AnsiConsole.MarkupLine("[cyan]Running in demo mode (non-interactive)[/]");
            AnsiConsole.WriteLine();

            try
            {
                AnsiConsole.MarkupLine("[yellow]═══ Configuration Info ═══[/]");
                ShowConfigurationInfo(config, warehouseId);
                AnsiConsole.WriteLine();

                AnsiConsole.MarkupLine("[yellow]═══ Quick Query Demo ═══[/]");
                await ExecuteQuickQuery(client, warehouseId);
                AnsiConsole.WriteLine();

                var demoLimit = int.TryParse(Environment.GetEnvironmentVariable("DEMO_LIMIT"), out var limit) ? limit : 10;
                AnsiConsole.MarkupLine($"[yellow]═══ Query Dataset Demo ({demoLimit} rows) ═══[/]");
                await StreamDatasetDemo(client, warehouseId, demoLimit);
                AnsiConsole.WriteLine();

                AnsiConsole.MarkupLine("[green]✓ Demo completed successfully![/]");
                return 0;
            }
            catch (DatabricksAuthenticationException ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Authentication failed:[/] {ex.Message}");
                return 1;
            }
            catch (DatabricksException ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Databricks error:[/] {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Error:[/] {ex.Message}");
                return 1;
            }
        }

        // Main menu loop (interactive mode)
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
                AnsiConsole.MarkupLine($"[red]✗ Authentication failed:[/] {ex.Message}");
            }
            catch (DatabricksException ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Databricks error:[/] {ex.Message}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Error:[/] {ex.Message}");
            }

            AnsiConsole.WriteLine();
        }
    }

    static DatabricksConfig? GetConfiguration()
    {
        var workspaceUrl = Environment.GetEnvironmentVariable("DATABRICKS_WORKSPACE_URL");
        var accessToken = Environment.GetEnvironmentVariable("DATABRICKS_TOKEN");
        var demoMode = Environment.GetEnvironmentVariable("DEMO_MODE") == "true";

        if (string.IsNullOrEmpty(workspaceUrl))
        {
            AnsiConsole.MarkupLine("[yellow]⚠[/] DATABRICKS_WORKSPACE_URL not set");
            workspaceUrl = AnsiConsole.Ask<string>("Enter [green]workspace URL[/]:");
        }

        // In demo mode, use PAT from environment variable
        if (demoMode)
        {
            return new DatabricksConfig
            {
                WorkspaceUrl = workspaceUrl,
                AccessToken = accessToken
            };
        }

        // Interactive mode - let user choose authentication method
        var authMethod = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Choose [green]authentication method[/]:")
                .AddChoices(new[] {
                    "Azure Entra ID (DefaultAzureCredential)",
                    "Personal Access Token (PAT)"
                }));

        if (authMethod.StartsWith("Azure Entra ID"))
        {
            return new DatabricksConfig
            {
                WorkspaceUrl = workspaceUrl,
                Credential = new DefaultAzureCredential()
            };
        }
        else
        {
            // PAT authentication
            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter [green]Personal Access Token[/]:")
                        .Secret());
            }

            return new DatabricksConfig
            {
                WorkspaceUrl = workspaceUrl,
                AccessToken = accessToken
            };
        }
    }

    static string GetAuthMethodName(DatabricksConfig config)
    {
        if (config.Credential == null)
            return "Personal Access Token";

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

    static async Task StreamDatasetDemo(SqlWarehouseClient client, string warehouseId, int limit)
    {
        // Use a more universally available sample dataset
        var sql = Environment.GetEnvironmentVariable("DEMO_QUERY")
            ?? $"SELECT * FROM `samples`.`accuweather`.`forecast_daily_calendar_metric` LIMIT {limit}";

        AnsiConsole.MarkupLine($"[dim]Executing:[/] {sql}");
        AnsiConsole.WriteLine();

        // Execute query and display results
        var stopwatch = Stopwatch.StartNew();
        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Executing query...", async ctx =>
            {
                return await client.ExecuteQueryAsync(warehouseId, sql);
            });
        stopwatch.Stop();

        AnsiConsole.MarkupLine($"[green]✓[/] Retrieved {result.Rows.Count} rows in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        AnsiConsole.WriteLine();

        // Display first 10 rows in a table
        AnsiConsole.MarkupLine($"[yellow]First {Math.Min(10, result.Rows.Count)} rows:[/]");
        DisplayResultsTable(result);
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
            table.AddColumn(new TableColumn($"[blue]{column.Name}[/]").Centered());
        }

        // Add rows
        foreach (var row in result.Rows.Take(20))
        {
            var values = row.Columns.Select(col => row.GetString(col) ?? "NULL").ToArray();
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
            table.AddColumn(new TableColumn($"[blue]{column.Name}[/]\n[dim]{column.Type}[/]"));
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

                // Truncate long values
                return value.Length > 30 ? value.Substring(0, 27) + "..." : value;
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
        var panel = new Panel(
            new Markup($"""
            [blue]Workspace URL:[/] {config.WorkspaceUrl}
            [blue]Warehouse ID:[/] {warehouseId}
            [blue]Authentication:[/] {GetAuthMethodName(config)}
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
