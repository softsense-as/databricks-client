# Python Console Example

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
