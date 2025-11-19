"""Main console application for Databricks SQL Client demo."""

import os
import sys
import time
from typing import Optional

from azure.identity import DefaultAzureCredential, InteractiveBrowserCredential
from rich.console import Console
from rich.panel import Panel
from rich.prompt import Prompt, IntPrompt
from rich.table import Table
from rich.progress import Progress, SpinnerColumn, TextColumn, BarColumn, TaskProgressColumn

from softsense_databricks_sqlclient import DatabricksConfig, SqlWarehouseClient
from softsense_databricks_sqlclient.exceptions import (
    DatabricksException,
    DatabricksAuthenticationException,
)

console = Console()


def get_configuration() -> Optional[DatabricksConfig]:
    """Get Databricks configuration from environment or user input."""
    workspace_url = os.getenv("DATABRICKS_WORKSPACE_URL")
    access_token = os.getenv("DATABRICKS_TOKEN")
    demo_mode = os.getenv("DEMO_MODE") == "true"

    if not workspace_url:
        console.print("[yellow]⚠[/yellow] DATABRICKS_WORKSPACE_URL not set")
        workspace_url = Prompt.ask("[green]Enter workspace URL[/green]")

    # In demo mode, use InteractiveBrowserCredential
    if demo_mode:
        return DatabricksConfig(
            workspace_url=workspace_url,
            credential=InteractiveBrowserCredential(),
        )

    # Try Azure Entra first, fallback to PAT
    try:
        credential = DefaultAzureCredential()
        return DatabricksConfig(
            workspace_url=workspace_url,
            credential=credential,
        )
    except Exception:
        if not access_token:
            console.print(
                "[yellow]⚠[/yellow] Azure Entra failed and DATABRICKS_TOKEN not set"
            )
            access_token = Prompt.ask(
                "[green]Enter access token[/green]", password=True
            )

        return DatabricksConfig(
            workspace_url=workspace_url,
            access_token=access_token,
        )


def get_auth_method_name(config: DatabricksConfig) -> str:
    """Get the authentication method name."""
    if config.credential is None:
        return "Personal Access Token"

    credential_type = type(config.credential).__name__
    if credential_type == "InteractiveBrowserCredential":
        return "Azure Entra ID (Interactive Browser)"
    elif credential_type == "DefaultAzureCredential":
        return "Azure Entra ID (Default Credential)"
    else:
        return f"Azure Entra ID ({credential_type})"


def execute_quick_query(client: SqlWarehouseClient, warehouse_id: str) -> None:
    """Execute a quick query to show current timestamp, user, and database."""
    sql = "SELECT current_timestamp() as timestamp, current_user() as user, current_database() as database LIMIT 10"

    console.print(f"[dim]Executing:[/dim] {sql}")
    console.print()

    start_time = time.time()
    with console.status("[bold green]Executing query...", spinner="dots"):
        result = client.execute_query(warehouse_id, sql)
    elapsed = time.time() - start_time

    display_results_table(result)
    console.print(
        f"[green]✓[/green] Retrieved {len(result.rows)} rows in {elapsed:.2f} seconds"
    )


def stream_dataset(client: SqlWarehouseClient, warehouse_id: str) -> None:
    """Stream a large dataset with progress bar."""
    limit = IntPrompt.ask("How many rows to stream?", default=100)
    sql = f"SELECT * FROM samples.nyctaxi.trips LIMIT {limit}"

    console.print(f"[dim]Streaming:[/dim] {sql}")
    console.print()

    row_count = 0
    start_time = time.time()

    with Progress(
        TextColumn("[progress.description]{task.description}"),
        BarColumn(),
        TaskProgressColumn(),
        SpinnerColumn(),
        console=console,
    ) as progress:
        task = progress.add_task("[green]Streaming rows", total=limit)

        for _ in client.execute_query_stream(warehouse_id, sql):
            row_count += 1
            progress.update(task, advance=1)

    elapsed = time.time() - start_time
    console.print(f"[green]✓[/green] Streamed {row_count} rows in {elapsed:.2f} seconds")


def stream_dataset_demo(client: SqlWarehouseClient, warehouse_id: str, limit: int) -> None:
    """Stream a dataset with a fixed limit (for demo mode)."""
    # Use a more universally available sample dataset
    sql = os.getenv("DEMO_QUERY") or f"SELECT * FROM `samples`.`accuweather`.`forecast_daily_calendar_metric` LIMIT {limit}"

    console.print(f"[dim]Executing:[/dim] {sql}")
    console.print()

    # Execute query and display results
    start_time = time.time()
    with console.status("[bold green]Executing query...", spinner="dots"):
        result = client.execute_query(warehouse_id, sql)
    elapsed = time.time() - start_time

    console.print(f"[green]✓[/green] Retrieved {len(result.rows)} rows in {elapsed:.2f} seconds")
    console.print()

    # Display first 10 rows in a table
    console.print(f"[yellow]First {min(10, len(result.rows))} rows:[/yellow]")
    display_results_table(result)


def explore_tables(client: SqlWarehouseClient, warehouse_id: str) -> None:
    """Explore available tables in the current database."""
    sql = "SHOW TABLES"

    start_time = time.time()
    with console.status("[bold green]Fetching tables...", spinner="dots"):
        result = client.execute_query(warehouse_id, sql)
    elapsed = time.time() - start_time

    if not result.rows:
        console.print("[yellow]No tables found in current database[/yellow]")
        return

    table = Table(title="[yellow]Available Tables[/yellow]", show_header=True)

    # Add columns
    for column in result.columns:
        table.add_column(f"[blue]{column.name}[/blue]", justify="center")

    # Add rows (limit to 20)
    for row in result.rows[:20]:
        values = [row.get_string(col) or "NULL" for col in row.columns]
        table.add_row(*values)

    console.print(table)

    if len(result.rows) > 20:
        console.print(f"[dim]Showing 20 of {len(result.rows)} tables[/dim]")

    console.print(f"[green]✓[/green] Retrieved {len(result.rows)} rows in {elapsed:.2f} seconds")


def execute_custom_query(client: SqlWarehouseClient, warehouse_id: str) -> None:
    """Execute a custom SQL query."""
    sql = Prompt.ask("[green]Enter SQL query[/green]", default="SELECT 1 as test")
    console.print()

    start_time = time.time()
    with console.status("[bold green]Executing query...", spinner="dots"):
        result = client.execute_query(warehouse_id, sql)
    elapsed = time.time() - start_time

    display_results_table(result)
    console.print(f"[green]✓[/green] Retrieved {len(result.rows)} rows in {elapsed:.2f} seconds")


def display_results_table(result) -> None:
    """Display query results as a table."""
    if not result.columns:
        console.print("[yellow]No columns in result[/yellow]")
        return

    if not result.rows:
        console.print("[yellow]No rows in result[/yellow]")
        return

    table = Table(show_header=True)

    # Limit columns to display (max 10 columns to fit in console)
    max_columns = 10
    columns_to_show = result.columns[:max_columns]

    # Add columns
    for column in columns_to_show:
        table.add_column(
            f"[blue]{column[0]}[/blue]\n[dim]{column[1]}[/dim]"
        )

    # Add rows (limit to 10 for display)
    rows_to_show = min(10, len(result.rows))
    for row in result.rows[:rows_to_show]:
        values = []
        for col_name, _ in columns_to_show:
            value = row.get(col_name)
            if value is None or value == "":
                values.append("[dim]NULL[/dim]")
            else:
                # Truncate long values
                str_value = str(value)
                values.append(str_value[:27] + "..." if len(str_value) > 30 else str_value)
        table.add_row(*values)

    console.print(table)

    if len(result.columns) > max_columns:
        console.print(f"[dim]Showing {max_columns} of {len(result.columns)} columns[/dim]")

    if len(result.rows) > rows_to_show:
        console.print(f"[dim]Showing {rows_to_show} of {len(result.rows)} rows[/dim]")


def show_configuration_info(config: DatabricksConfig, warehouse_id: str) -> None:
    """Show current configuration information."""
    info = f"""
[blue]Workspace URL:[/blue] {config.workspace_url}
[blue]Warehouse ID:[/blue] {warehouse_id}
[blue]Authentication:[/blue] {get_auth_method_name(config)}
[blue]Timeout:[/blue] {config.timeout_seconds}s
[blue]Max Retries:[/blue] {config.max_retries}
[blue]Polling Interval:[/blue] {config.polling_interval_milliseconds}ms
    """

    panel = Panel(info.strip(), title="[yellow]Configuration[/yellow]", border_style="green")
    console.print(panel)


def main() -> int:
    """Main entry point for the application."""
    # Display banner
    console.print("[bold blue]" + "=" * 60 + "[/bold blue]")
    console.print("[bold blue]         DATABRICKS SQL CLIENT DEMO[/bold blue]")
    console.print("[bold blue]" + "=" * 60 + "[/bold blue]")
    console.print()

    # Get configuration
    config = get_configuration()
    if config is None:
        console.print("[red]✗[/red] Configuration failed. Please set environment variables.")
        return 1

    # Create client
    client = SqlWarehouseClient(config)

    # Get warehouse ID
    warehouse_id = os.getenv("DATABRICKS_WAREHOUSE_ID")
    if not warehouse_id:
        warehouse_id = Prompt.ask("Enter [green]SQL Warehouse ID[/green]")

    # Show connection status
    console.print(f"[green]✓[/green] Connected to: [blue]{config.workspace_url}[/blue]")
    console.print(
        f"[green]✓[/green] Authentication: [blue]{get_auth_method_name(config)}[/blue]"
    )
    console.print()

    # Check if running in demo mode
    demo_mode = os.getenv("DEMO_MODE") == "true"

    if demo_mode:
        # Run automated demo
        console.print("[cyan]Running in demo mode (non-interactive)[/cyan]")
        console.print()

        try:
            console.print("[yellow]═══ Configuration Info ═══[/yellow]")
            show_configuration_info(config, warehouse_id)
            console.print()

            console.print("[yellow]═══ Quick Query Demo ═══[/yellow]")
            execute_quick_query(client, warehouse_id)
            console.print()

            demo_limit = int(os.getenv("DEMO_LIMIT", "10"))
            console.print(f"[yellow]═══ Query Dataset Demo ({demo_limit} rows) ═══[/yellow]")
            stream_dataset_demo(client, warehouse_id, demo_limit)
            console.print()

            console.print("[green]✓ Demo completed successfully![/green]")
            return 0
        except DatabricksAuthenticationException as ex:
            console.print(f"[red]✗ Authentication failed:[/red] {ex}")
            return 1
        except DatabricksException as ex:
            console.print(f"[red]✗ Databricks error:[/red] {ex}")
            return 1
        except Exception as ex:
            console.print(f"[red]✗ Error:[/red] {ex}")
            return 1

    # Main menu loop
    while True:
        console.print("[yellow]What would you like to do?[/yellow]")
        console.print("1. Quick Query")
        console.print("2. Stream Large Dataset")
        console.print("3. Explore Tables")
        console.print("4. Custom Query")
        console.print("5. Configuration Info")
        console.print("6. Exit")
        console.print()

        choice = Prompt.ask("Enter your choice", choices=["1", "2", "3", "4", "5", "6"])
        console.print()

        try:
            if choice == "1":
                execute_quick_query(client, warehouse_id)
            elif choice == "2":
                stream_dataset(client, warehouse_id)
            elif choice == "3":
                explore_tables(client, warehouse_id)
            elif choice == "4":
                execute_custom_query(client, warehouse_id)
            elif choice == "5":
                show_configuration_info(config, warehouse_id)
            elif choice == "6":
                console.print("[green]Goodbye![/green]")
                return 0

        except DatabricksAuthenticationException as ex:
            console.print(f"[red]✗ Authentication failed:[/red] {ex}")
        except DatabricksException as ex:
            console.print(f"[red]✗ Databricks error:[/red] {ex}")
        except Exception as ex:
            console.print(f"[red]✗ Error:[/red] {ex}")

        console.print()


if __name__ == "__main__":
    sys.exit(main())
