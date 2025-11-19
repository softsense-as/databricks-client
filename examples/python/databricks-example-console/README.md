# Python Console Example

> [!WARNING]
> This example is a Work In Progress (WIP) and may not be fully functional.

Interactive console application with Rich UI.

## Features

- Interactive menu system
- Progress bars and styled tables
- Quick query, streaming, table exploration
- Azure Entra ID and PAT authentication

## Quick Start

```bash
pip install -e .
databricks-demo
```

## Configuration

> [!NOTE]
> When running this application from the AspireHost, environment variables are automatically provided. Manual configuration is only needed when running standalone.

### Using .env file

Create a `.env` file in the project directory (already gitignored):

```bash
DatabricksConfig__WorkspaceUrl=https://your-workspace.azuredatabricks.net
DatabricksConfig__WarehouseId=your-warehouse-id
DatabricksConfig__AccessToken=your-token
```

Then run with `uv`:

```bash
uv run databricks-demo
```

### Using environment variables

```bash
# Windows
set DatabricksConfig__WorkspaceUrl=https://your-workspace.azuredatabricks.net
set DatabricksConfig__WarehouseId=your-warehouse-id
set DatabricksConfig__AccessToken=your-token

# Linux/macOS
export DatabricksConfig__WorkspaceUrl=https://your-workspace.azuredatabricks.net
export DatabricksConfig__WarehouseId=your-warehouse-id
export DatabricksConfig__AccessToken=your-token
```

## License

Apache 2.0
