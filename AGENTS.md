# AI Agent Instructions

## Project Overview

Databricks SQL Warehouse SDK with C# core and Python wrapper.

**Repository**: https://github.com/softsense/databricks-client
**Structure**:
- `source/csharp/` - .NET 10 implementation
- `source/python/` - Python wrapper via pythonnet
- `examples/` - Console applications

## Architecture Principles

1. **Single source of truth**: All HTTP, auth, retry logic in C#
2. **Python wraps .NET**: Not a standalone Python implementation
3. **Performance first**: Efficient streaming, no unnecessary allocations
4. **AOT compatible**: No reflection-based serialization

## Current Implementation

### Packages
- `SoftSense.Databricks.Core` - HTTP client, auth, config, exceptions
- `SoftSense.Databricks.SqlClient` - SQL Warehouse queries
- `softsense-databricks-sqlclient` - Python wrapper

### Query Methods
```csharp
ExecuteQueryAsync()              // Full result as QueryResult
ExecuteQueryJsonAsync()          // Full result as JSON array of objects
ExecuteQueryStreamAsync()        // Stream raw JSON arrays
ExecuteQueryStreamNdjsonAsync()  // Stream NDJSON with column names
```

### Authentication
- Azure Entra ID (preferred): `Credential` property
- Personal Access Token: `AccessToken` property

## Critical Implementation Rules

### Streaming Requirements
- ✅ Use `Utf8JsonReader` to extract raw JSON
- ✅ Use `HttpCompletionOption.ResponseHeadersRead`
- ✅ Stream data as received
- ❌ NEVER use `JsonDocument.Parse()` on full response
- ❌ NEVER deserialize then re-serialize in streaming

### JSON Handling
- Use `Utf8JsonWriter` for efficient output
- No `JsonSerializer.SerializeToElement` (not AOT-compatible)
- Write values directly: `writer.WriteNumberValue()`, `writer.WriteStringValue()`
- All API models use snake_case via `[JsonPropertyName]`

### Error Handling
```csharp
try { }
catch (DatabricksAuthenticationException) { } // 401
catch (DatabricksRateLimitException) { }      // 429
catch (DatabricksHttpException) { }           // Other HTTP
catch (DatabricksException) { }               // General
```

## Code Style

### C# Requirements
- `required` keyword for mandatory properties
- `sealed` classes by default
- Async methods end with `Async`
- `[EnumeratorCancellation]` on async enumerables
- XML docs on public APIs
- Nullable reference types enabled
- Warnings treated as errors
- AOT-compatible code only

### Python Requirements
- Type hints on all functions
- snake_case for all identifiers
- Context managers (`__enter__`/`__exit__`)
- Must pass `mypy --strict`
- PEP 8 compliant

## Build Commands

```bash
# C# Build
cd source/csharp
dotnet build
dotnet test
dotnet format

# Python Build
cd source/python/softsense-databricks-sqlclient
pip install -e ".[dev]"
pytest tests/
ruff check src/
black src/
mypy src/
```

## CI/CD Workflows

- `.github/workflows/ci.yml` - Main orchestrator
- `.github/workflows/dotnet-build.yml` - C# build, publish to GitHub Packages
- `.github/workflows/python-build.yml` - Python build, publish to PyPI

Triggers:
- `dotnet-build.yml` - On `source/csharp/**` changes
- `python-build.yml` - On `source/python/**` or `source/csharp/**` changes
- Both publish on GitHub release events

## File Organization

```
source/
├── csharp/
│   ├── SoftSense.Databricks.Core/
│   │   ├── Configuration/
│   │   ├── Exceptions/
│   │   ├── Http/
│   │   └── Models/
│   ├── SoftSense.Databricks.SqlClient/
│   │   ├── Models/
│   │   └── SqlWarehouseClient.cs
│   └── SoftSense.Databricks.Tests/
└── python/
    └── softsense-databricks-sqlclient/
        ├── src/softsense_databricks_sqlclient/
        │   ├── __init__.py
        │   ├── client.py
        │   └── exceptions.py
        └── tests/
```

## Testing Requirements

### C# Tests (xUnit)
- Config validation
- Exception handling
- Retry logic
- HTTP client behavior

### Python Tests (pytest)
- Config validation
- .NET interop
- Exception wrapping
- DataFrame conversion

## Common Tasks

### Adding a New Query Method
1. Implement in `SqlWarehouseClient.cs`
2. Use `Utf8JsonReader/Writer` for JSON
3. Add XML documentation
4. Add to Python wrapper in `client.py`
5. Add tests in both C# and Python
6. Update README examples

### Adding a New API Client
1. Create `SoftSense.Databricks.{Service}/`
2. Add models in `Models/`
3. Implement `{Service}Client.cs`
4. Add tests in `SoftSense.Databricks.Tests/`
5. Create Python wrapper
6. Update documentation

## Python-Specific Notes

### .NET Assembly Loading
```python
# Development
relative_path = "../../../../csharp/.../bin/Debug/net10.0/"

# Production
importlib.resources.files(__package__).joinpath("lib")
```

### Calling .NET Async Methods
```python
result = dotnet_method.GetAwaiter().GetResult()
```

### Type Conversion
- .NET `List<object>` → Python `list`
- .NET `Dictionary` → Python `dict`
- .NET `QueryResult` → Python `QueryResult` wrapper
- .NET exceptions → Python exceptions

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `WorkspaceUrl` | string | Required | Databricks workspace URL |
| `WarehouseId` | string | Optional | Default warehouse ID |
| `Credential` | TokenCredential | null | Azure Entra credential |
| `AccessToken` | string | null | PAT token |
| `TimeoutSeconds` | int | 300 | HTTP timeout |
| `MaxRetries` | int | 3 | Retry attempts |

## Known Limitations

1. Python async uses `.GetAwaiter().GetResult()` - not true async
2. Python bundles .NET DLLs in `lib/` directory
3. Requires .NET 10 runtime for Python package

## Future APIs (Planned)

- `SoftSense.Databricks.UnityCatalog` - Catalog operations
- `SoftSense.Databricks.Workspace` - Notebooks, repos
- `SoftSense.Databricks.Jobs` - Job management
- `SoftSense.Databricks.Clusters` - Cluster lifecycle

## Support & Documentation

- **Issues**: GitHub Issues
- **Main README**: `/README.md`
- **Python README**: `/source/python/softsense-databricks-sqlclient/README.md`
- **Examples**: `/examples/`

## Key Takeaways for AI Agents

1. **Never batch data in streaming** - Use `Utf8JsonReader` properly
2. **AOT compatibility is mandatory** - No reflection
3. **C# is the source** - Python just wraps it
4. **Performance matters** - Efficient JSON parsing, minimal allocations
5. **Follow existing patterns** - Consistency across codebase
