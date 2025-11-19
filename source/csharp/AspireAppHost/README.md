# Databricks Examples - Aspire AppHost

This is a .NET Aspire AppHost that orchestrates both the .NET and Python example console applications for the Databricks SQL Client SDK.

## Overview

The Aspire AppHost provides a unified way to run and manage both example applications with shared configuration and environment variables. It uses .NET Aspire 13.0 for modern cloud-native application orchestration.

## Features

- **Unified Configuration**: Centralized configuration for all example applications
- **Environment Variable Management**: Shared Databricks credentials across both .NET and Python apps
- **Dashboard**: Access the Aspire dashboard to monitor both applications
- **Python with uv Support**: Built-in support for Python virtual environments using uv
- **Secret Management**: Store Databricks tokens securely via configuration + .NET user secrets

## Prerequisites

- .NET 10 SDK
- Python 3.10 or higher
- uv (Python package installer) - install with: `pip install uv`
- Databricks workspace access
- SQL Warehouse ID

## Configuration

### Option 1: Using appsettings.Development.json

Edit `appsettings.Development.json` and update the `Databricks` section (these map directly to `DatabricksConfig`):

```json
{
  "Databricks": {
    "WorkspaceUrl": "https://your-workspace.azuredatabricks.net",
    "WarehouseId": "your-warehouse-id",
    "AccessToken": "your-token-or-store-in-user-secrets",
    "TimeoutSeconds": 300,
    "MaxRetries": 3,
    "PollingIntervalMilliseconds": 1000
  }
}
```

### Option 2: Using User Secrets

For better security, use .NET User Secrets:

```bash
cd source/csharp/AspireAppHost

dotnet user-secrets set "Databricks:WorkspaceUrl" "https://your-workspace.azuredatabricks.net"
dotnet user-secrets set "Databricks:WarehouseId" "your-warehouse-id"
dotnet user-secrets set "Databricks:AccessToken" "your-token"
```

### Option 3: Using Environment Variables

Set configuration values via environment variables before running. Use double underscores (`__`) to represent nested JSON paths:

#### Windows (PowerShell)

```powershell
$env:Databricks__WorkspaceUrl = "https://your-workspace.azuredatabricks.net"
$env:Databricks__WarehouseId = "your-warehouse-id"
$env:Databricks__AccessToken = "your-token"
```

#### Linux/macOS (Bash)

```bash
export Databricks__WorkspaceUrl="https://your-workspace.azuredatabricks.net"
export Databricks__WarehouseId="your-warehouse-id"
export Databricks__AccessToken="your-token"
```

## Running the AppHost

### From the command line

```bash
cd source/csharp/AspireAppHost
dotnet run
```

### From Visual Studio 2022

1. Open the solution: `source/csharp/SoftSense.Databricks.Client.sln`
2. Set `AspireAppHost` as the startup project
3. Press F5 to run

### What happens when you run

1. The Aspire dashboard will open in your browser (usually at `http://localhost:15888`)
2. Both the .NET and Python example applications will be available to start
3. All environment variables will be automatically configured
4. You can monitor logs, traces, and metrics from the dashboard

## Managed Applications

### 1. dotnet-example

- **Type**: .NET Console Application
- **Project**: Databricks.Examples.Console
- **Features**:
  - Spectre.Console for beautiful terminal UI
  - Interactive menu system
  - Azure Entra ID authentication

### 2. python-example

- **Type**: Python Application with uv
- **Location**: `../../examples/python/databricks-example-console`
- **Features**:
  - Rich library for terminal UI
  - Interactive menu system
  - Azure Entra ID authentication via pythonnet

## Environment Variables

Both applications receive the following environment variables:

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `DATABRICKS_WORKSPACE_URL` | Your Databricks workspace URL | Yes | - |
| `DATABRICKS_WAREHOUSE_ID` | SQL Warehouse ID to connect to | Yes | - |
| `DATABRICKS_TOKEN` | Personal Access Token (if not using Azure Entra) | No | - |
| `DEMO_MODE` | Run in non-interactive demo mode | No | `true` (in Aspire) |
| `DEMO_QUERY` | Custom query for demo mode | No | AccuWeather sample dataset |
| `DEMO_LIMIT` | Number of rows to stream in demo mode | No | `10` |

## Authentication

### Demo Mode (InteractiveBrowserCredential)

When running through Aspire in demo mode, both applications use **InteractiveBrowserCredential** which:

- Opens a browser for interactive sign-in
- Works with any Azure/Microsoft account
- Provides a secure, user-friendly authentication experience
- **No token configuration required**

### Interactive Mode (DefaultAzureCredential)

When running applications directly (not through Aspire), they use **DefaultAzureCredential** which tries multiple authentication methods in order:

1. Environment variables
2. Managed Identity
3. Visual Studio credentials
4. Azure CLI credentials (requires `az login`)
5. Azure PowerShell credentials
6. Interactive browser (as a fallback)

Ensure you're logged in with Azure CLI:

```bash
az login
```

### Personal Access Token (PAT)

Alternatively, set the `Databricks:AccessToken` configuration value (via appsettings, user secrets, or environment variables) to your Personal Access Token. This is useful when Azure Entra ID is not available.

## Aspire Dashboard

The Aspire dashboard provides:

- **Resources**: View and manage both applications
- **Console Logs**: See output from each application
- **Structured Logs**: View structured logging data
- **Traces**: View distributed traces (if configured)
- **Metrics**: Monitor application metrics (if configured)

Access the dashboard at: `http://localhost:15888` (default port)

## Demo Mode

When running through Aspire, both applications automatically run in **demo mode** which executes a pre-defined sequence of operations instead of prompting for user input.

### Authentication in Demo Mode

Demo mode uses **InteractiveBrowserCredential** for Azure Entra ID authentication, which will:
 
1. Open your default browser
2. Prompt you to sign in with your Azure/Microsoft account
3. Automatically obtain and use the authentication token

This provides a secure, user-friendly authentication experience without needing to configure tokens or credentials.

**Note**: The `DATABRICKS_TOKEN` parameter is not required when using demo mode with InteractiveBrowserCredential.

### Default Demo Behavior

The demo mode executes the following sequence:

1. **Configuration Info** - Displays workspace URL, warehouse ID, authentication method, and settings
2. **Quick Query** - Executes a simple query showing current timestamp, user, and database
3. **Query Dataset** - Executes a query for **10 rows** (configurable via `DEMO_LIMIT`) and displays results in a formatted table

Default dataset query:
 
```sql
SELECT * FROM `samples`.`accuweather`.`forecast_daily_calendar_metric` LIMIT 10
```

The results are displayed in a beautiful table with:

- Column names and data types
- Up to 10 rows of data
- Proper formatting and alignment

You can override the demo query by setting the `DEMO_QUERY` environment variable on both managed applications. Update `AppHost.cs` after the resource definitions:

```csharp
const string demoQuery = "SELECT * FROM your_table LIMIT 50";

dotnetExample.WithEnvironment("DEMO_QUERY", demoQuery);
pythonExample.WithEnvironment("DEMO_QUERY", demoQuery);
```

### Interactive Mode

To run the applications in interactive mode (with full menu system), run them directly from the command line instead of through Aspire.

## Virtual Environment Setup with uv

The Python example uses **uv** for fast package management via `.WithUv()` in Aspire 13. The virtual environment is located at `.venv` within the Python example directory.

Aspire will automatically:

1. Use **uv** for package management (faster than pip)
2. Create the virtual environment if it doesn't exist
3. Install dependencies from `pyproject.toml` using uv
4. Activate the environment before running the application

You can pre-create the environment manually:

```bash
cd source/examples/python/databricks-example-console
uv venv .venv
uv pip install -e .
```

### Why uv?

The AppHost uses `.WithUv()` which provides:

- **Faster package installation** (10-100x faster than pip)
- **Better dependency resolution**
- **Reliable virtual environment management**
- **Native support in Aspire 13**

## Troubleshooting

### Python application fails to start

1. Ensure uv is installed: `pip install uv`
2. Manually create the virtual environment: `cd source/examples/python/databricks-example-console && uv venv .venv`
3. Install dependencies: `uv pip install -e .`

### .NET application fails to start

1. Ensure all project references are correct
2. Build the solution: `dotnet build source/csharp/SoftSense.Databricks.Client.sln`
3. Check that the example project builds independently

### Missing configuration

If the `Databricks` settings are missing, you'll see errors in the dashboard. Use one of the configuration methods above.

## Project Structure

```text
AspireAppHost/
├── AppHost.cs                       # Main Aspire configuration
├── AspireAppHost.csproj             # Project file with Aspire SDK
├── appsettings.json                 # Base configuration
├── appsettings.Development.json     # Development DatabricksConfig overrides
└── README.md                        # This file
```

## Development Workflow

1. **Start the AppHost**: `dotnet run` in the AspireAppHost directory
2. **Access Dashboard**: Open `http://localhost:15888` in your browser
3. **Start Applications**: Click "Start" on either application in the dashboard
4. **Monitor Logs**: View console output in the dashboard
5. **Stop Applications**: Click "Stop" or close the dashboard

## Advanced Configuration

### Custom Virtual Environment Path

Edit `AppHost.cs` to change the virtual environment path:

```csharp
.WithVirtualEnvironment("custom-venv-path")
```

### Additional Arguments

Add custom arguments to the Python application:

```csharp
.WithArgs("-m", "databricks_example.app", "--custom-arg")
```

### Resource Dependencies

Add dependencies between resources:

```csharp
var pythonExample = builder.AddPythonExecutable(...)
    .WaitFor(dotnetExample);  // Wait for .NET app to start first
```

## License

Apache 2.0 - See the main repository LICENSE file for details.

## Related Documentation

- [.NET Example README](../../examples/dotnet/Databricks.Examples.Console/README.md)
- [Python Example README](../../examples/python/databricks-example-console/README.md)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
