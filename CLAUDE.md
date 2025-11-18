# Claude Code Instructions

High-performance .NET SDK for Databricks SQL Warehouse with Python wrapper via pythonnet.

## Architecture

- **C# Core** (`source/csharp/`) - .NET 10, published to NuGet and GitHub Packages
- **Python Wrapper** (`source/python/`) - Calls .NET via pythonnet, published to PyPI
- **Single source of truth** - All logic in C#, Python provides Pythonic API

## Key Implementation Details

### Streaming: No Batching, No Deserialization
```csharp
public async IAsyncEnumerable<string> ExecuteQueryStreamAsync(...)
```
- Returns raw JSON strings
- Uses `Utf8JsonReader` for efficient parsing
- `HttpCompletionOption.ResponseHeadersRead` for immediate streaming
- **Never** use `JsonDocument.Parse()` on full response (batches everything)
- Extract JSON directly via `TokenStartIndex` and byte slicing

### Authentication
```csharp
public TokenCredential? Credential { get; init; }  // Azure Entra ID (preferred)
public string? AccessToken { get; init; }          // PAT (fallback)
```
- Token refresh in `DatabricksHttpClient.GetAccessTokenAsync()`
- Resource ID: `2ff814a6-3304-4ab8-85cb-cd0e6f879c1d`

### Retry Logic
- Exponential backoff: 2^attempt seconds
- Retries on: 429, 5xx, network errors
- Default max retries: 3

### JSON Serialization
- System.Text.Json with snake_case
- All models use `[JsonPropertyName("snake_case")]`
- AOT-compatible: No reflection-based serialization

### Python Bridge
- Locates .NET DLLs via relative path (dev) or `importlib.resources` (prod)
- Calls async methods: `.GetAwaiter().GetResult()`

## Coding Standards

### C#
- `required` for mandatory properties
- `sealed` classes by default
- Nullable reference types enabled
- Warnings as errors
- AOT-compatible only
- XML docs on public APIs

### Python
- Type hints everywhere
- snake_case (PEP 8)
- Must pass `mypy --strict`
- Context managers for resources

## Build & Test

```bash
# C#
cd source/csharp
dotnet build
dotnet test

# Python
cd source/python/softsense-databricks-sqlclient
pip install -e ".[dev]"
pytest tests/
ruff check src/
black src/
```

## CI/CD

- `dotnet-build.yml` - Publishes to GitHub Packages on release
- `python-build.yml` - Publishes to PyPI on release
- NuGet.org publishing currently disabled

## Critical Patterns to Follow

1. **No batching** - Stream data as received, never load full response
2. **Use Utf8JsonReader/Writer** - No reflection-based JSON serialization
3. **HTTP streaming** - Always use `ResponseHeadersRead`
4. **AOT compatibility** - Avoid dynamic code generation
5. **Single source** - Implement in C#, wrap in Python

## Exception Hierarchy

```
DatabricksException
├── DatabricksAuthenticationException (401)
├── DatabricksHttpException (HTTP errors)
└── DatabricksRateLimitException (429)
```

## What NOT to Do

❌ Use `JsonDocument.Parse(fullResponse)` - batches everything
❌ Deserialize then serialize in streaming
❌ Use reflection-based JSON serialization
❌ Forget `[EnumeratorCancellation]` on async enumerables
❌ Use `warehouseId` in Python (use `warehouse_id`)
❌ Create documentation files unless explicitly requested

## Quick Reference

**Config**: Either `Credential` (Azure Entra) or `AccessToken` (PAT) required
**Streaming**: `ExecuteQueryStreamAsync` returns `IAsyncEnumerable<string>`
**NDJSON**: `ExecuteQueryStreamNdjsonAsync` converts arrays to objects with column names
**JSON Objects**: `ExecuteQueryJsonAsync` returns full result as JSON array
**Timeout**: Default 300s, configurable via `TimeoutSeconds`
**Retries**: Default 3, configurable via `MaxRetries`
