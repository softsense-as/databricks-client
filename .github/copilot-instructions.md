# Databricks Client SDK - Development Guide

## Project Architecture

Dual-language SDK for Databricks HTTP APIs:
- **Core**: C# (.NET 10) in `source/csharp/` - published to NuGet
- **Python wrapper**: `source/python/` uses pythonnet for .NET interop
- **Single source**: All HTTP, retry, auth, serialization in C#

### Key Components
- `SoftSense.Databricks.Core/` - HTTP client, auth (PAT + Azure Entra ID), config, exceptions
- `SoftSense.Databricks.SqlClient/` - SQL Warehouse queries with streaming
- `softsense-databricks-sqlclient/` - Python wrapper calling .NET via pythonnet

## Critical Patterns

### Authentication: Dual Mode (PAT or Azure Entra ID)
```csharp
// DatabricksConfig supports EITHER:
public TokenCredential? Credential { get; init; }  // Azure Entra (preferred)
public string? AccessToken { get; init; }          // PAT (legacy)
```
- Azure Entra uses `TokenRequestContext` with resource ID `2ff814a6-3304-4ab8-85cb-cd0e6f879c1d`
- Token refresh happens in `DatabricksHttpClient.GetAccessTokenAsync()`
- Validation enforces one auth method in `DatabricksConfig.Validate()`

### JSON Serialization: snake_case Convention
All Databricks API models use `[JsonPropertyName("snake_case")]` attributes:
```csharp
[JsonPropertyName("warehouse_id")]
public required string WarehouseId { get; init; }
```
System.Text.Json with `PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower`

### Retry Logic
- `MaxRetries` defaults to 3 (configurable)
- Exponential backoff: 2^attempt seconds
- Retries on: 429 (rate limit), 5xx errors, network errors
- See `DatabricksHttpClient.SendRequestInternalAsync()`

### Streaming Queries
```csharp
public async IAsyncEnumerable<string> ExecuteQueryStreamAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
```
- Yields raw JSON strings without deserialization
- Uses `Utf8JsonReader` for efficient parsing
- No batching - streams data as received
- Use `HttpCompletionOption.ResponseHeadersRead` for immediate streaming

### Python-to-.NET Bridge
Python locates .NET DLLs:
1. **Development**: Relative path to `bin/Debug/net10.0/`
2. **Production**: `importlib.resources.files()` for packaged assemblies in `lib/`

Python calls .NET async methods: `.GetAwaiter().GetResult()`

## Exception Hierarchy
All custom exceptions inherit from `DatabricksException`:
- `DatabricksAuthenticationException` - 401 errors or token acquisition failures
- `DatabricksHttpException` - HTTP errors with `StatusCode` and `ResponseBody` properties
- `DatabricksRateLimitException` - 429 rate limit (extends `DatabricksHttpException`)

## Build & Test

### C#
```bash
cd source/csharp
dotnet build
dotnet test
dotnet format
```

### Python
```bash
cd source/python/softsense-databricks-sqlclient
pip install -e ".[dev]"
pytest tests/
ruff check src/
black src/
mypy src/
```

## CI/CD
- `ci.yml` - Orchestrator with path-based triggers
- `dotnet-build.yml` - Triggers on C# changes, publishes to NuGet
- `python-build.yml` - Triggers on Python/C# changes, publishes to PyPI

## Coding Conventions

### C#
- `required` keyword for mandatory properties
- XML doc comments for public APIs
- `sealed` classes by default
- Async methods end with `Async`
- Nullable reference types enabled
- Warnings treated as errors
- AOT-compatible code only

### Python
- Type hints on all functions
- `dataclass` for config objects
- Context manager support
- PEP 8 naming: `warehouse_id` not `warehouseId`
- snake_case everywhere
- Must pass `mypy --strict`

## Adding New APIs

1. Create C# models in `SoftSense.Databricks.{Service}/Models/`
2. Add client in `{Service}Client.cs`
3. Add Python wrapper in `src/`
4. Update READMEs
5. Add tests

Future APIs planned:
- UnityCatalog - Catalog, schema, table operations
- Workspace - Notebooks, repos, secrets
- Jobs - Job management
- Clusters - Cluster lifecycle

## Testing

### C# (xUnit)
```bash
dotnet test
```
Coverage: config validation, exceptions, retry logic

### Python (pytest)
```bash
pytest tests/
```
Coverage: config validation, exceptions, .NET interop

## Known Limitations
- Python async uses `.GetAwaiter().GetResult()` - not true async
- Python bundles .NET DLLs in `lib/` directory
