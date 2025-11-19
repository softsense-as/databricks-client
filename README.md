# Databricks Client SDK

High-performance .NET SDK for Databricks SQL Warehouse.

[![.NET Build and Publish](https://github.com/softsense-as/databricks-client/actions/workflows/dotnet-build.yml/badge.svg?branch=main)](https://github.com/softsense-as/databricks-client/actions/workflows/dotnet-build.yml)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

## Features

- Execute SQL queries against Databricks SQL Warehouses
- Stream large result sets efficiently
- Azure Entra ID and PAT authentication
- Async/await support
- Automatic retry with exponential backoff
- AOT-compatible

## Installation

```bash
dotnet add package SoftSense.Databricks.Core
dotnet add package SoftSense.Databricks.SqlClient
```

## Quick Start

### C#

```csharp
using Azure.Identity;
using SoftSense.Databricks.Core.Configuration;
using SoftSense.Databricks.SqlClient;

var config = new DatabricksConfig
{
    WorkspaceUrl = "https://adb-123456789.azuredatabricks.net",
    Credential = new DefaultAzureCredential(),
    WarehouseId = "your-warehouse-id"
};

using var client = new SqlWarehouseClient(config);

// Execute query
var result = await client.ExecuteQueryAsync("SELECT * FROM table");

// Stream large results
await foreach (var row in client.ExecuteQueryStreamAsync("SELECT * FROM large_table"))
{
    ProcessRow(row);
}
```

## Authentication

### Azure Entra ID (Recommended)

```csharp
var config = new DatabricksConfig
{
    WorkspaceUrl = "https://adb-123456789.azuredatabricks.net",
    Credential = new DefaultAzureCredential()
};
```

### Personal Access Token

```csharp
var config = new DatabricksConfig
{
    WorkspaceUrl = "https://your-workspace.databricks.com",
    AccessToken = Environment.GetEnvironmentVariable("DatabricksConfig__AccessToken")
};
```

### Environment Variables

Configuration can be loaded from environment variables using .NET configuration notation:

```bash
# Windows
set DatabricksConfig__WorkspaceUrl=https://your-workspace.databricks.com
set DatabricksConfig__WarehouseId=your-warehouse-id
set DatabricksConfig__AccessToken=your-token

# Linux/macOS
export DatabricksConfig__WorkspaceUrl=https://your-workspace.databricks.com
export DatabricksConfig__WarehouseId=your-warehouse-id
export DatabricksConfig__AccessToken=your-token
```

Then load with configuration builder:

```csharp
using Microsoft.Extensions.Configuration;
using SoftSense.Databricks.Core.Configuration;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var config = configuration.GetSection("DatabricksConfig").Get<DatabricksConfig>();
```

## Configuration

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `WorkspaceUrl` | string | Required | Databricks workspace URL |
| `WarehouseId` | string | Optional | Default warehouse ID |
| `Credential` | TokenCredential | null | Azure Entra ID credential |
| `AccessToken` | string | null | Personal Access Token |
| `TimeoutSeconds` | int | 300 | HTTP request timeout |
| `MaxRetries` | int | 3 | Max retry attempts |

**Note:** Either `Credential` or `AccessToken` is required.

## Error Handling

```csharp
using SoftSense.Databricks.Core.Exceptions;

try
{
    var result = await client.ExecuteQueryAsync(sql);
}
catch (DatabricksAuthenticationException ex)
{
    Console.WriteLine($"Auth failed: {ex.Message}");
}
catch (DatabricksRateLimitException ex)
{
    Console.WriteLine($"Rate limited. Retry after: {ex.RetryAfter}");
}
catch (DatabricksHttpException ex)
{
    Console.WriteLine($"HTTP {ex.StatusCode}: {ex.Message}");
}
```

## Building from Source

```bash
cd source/csharp
dotnet restore
dotnet build
dotnet test
```

## Benchmarks

Performance benchmarks comparing batched vs streamed query execution are available in `source/csharp/SoftSense.Databricks.Benchmarks`.

```bash
cd source/csharp/SoftSense.Databricks.Benchmarks
dotnet run -c Release
```

See [benchmark documentation](source/csharp/SoftSense.Databricks.Benchmarks/README.md) for details.

## License

Apache License 2.0 - see [LICENSE](LICENSE) file.

## Support

- Issues: [GitHub Issues](https://github.com/softsense/databricks-client/issues)
- Discussions: [GitHub Discussions](https://github.com/softsense/databricks-client/discussions)
