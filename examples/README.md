# Examples

Example applications demonstrating the Databricks SQL Client SDK.

## .NET Console (Minimal)

**Location**: `dotnet/SoftSense.Examples.ConsoleMinimal/`

Simple console app with multiple fetch methods.

```bash
cd dotnet/SoftSense.Examples.ConsoleMinimal
dotnet run
```

## .NET Console (Full)

**Location**: `dotnet/SoftSense.Examples.Console/`

Feature-rich console with Spectre.Console UI, interactive menus, and progress bars.

```bash
cd dotnet/SoftSense.Examples.Console
dotnet run
```

## Python Console

**Location**: `python/databricks-example-console/`

Python console app with Rich UI and interactive menus.

```bash
cd python/databricks-example-console
pip install -e .
databricks-demo
```

## Configuration

```bash
# Windows
set DatabricksConfig__WorkspaceUrl=https://your-workspace.azuredatabricks.net
set DatabricksConfig__WarehouseId=your-warehouse-id
set DatabricksConfig__AccessToken=your-token

# Linux/macOS
export DatabricksConfig__WorkspaceUrl=https://your-workspace.azuredatabricks.net
export DatabricksConfig__WarehouseId=your-warehouse-id
export DatabricksConfig__AccessToken=your-token

# Authentication - Option 1: Azure Entra ID (recommended)
az login

# Authentication - Option 2: Use AccessToken above
```

## License

Apache 2.0
