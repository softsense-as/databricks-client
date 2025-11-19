# AI Agent Playbook

This document is the single source of truth for autonomous or semi-autonomous agents working in the **Databricks SQL Warehouse SDK** repository. Read it end-to-end before editing code.

---

## 1. Quick Reference

| Area | Details |
| --- | --- |
| Mono-repo URL | <https://github.com/softsense/databricks-client> |
| Core Tech | .NET 10 (C#), Python 3.10+, pythonnet bridge |
| Primary Products | `SoftSense.Databricks.Core`, `SoftSense.Databricks.SqlClient`, `softsense-databricks-sqlclient` |
| Key Directories | `source/csharp`, `source/python`, `examples/` |
| Examples | `examples/dotnet/SoftSense.Examples.Console`, `docs/examples/python_example.py` |

> **Golden Rule:** C# is the single source of truth for HTTP/auth/serialization. Python is only a wrapper over the .NET assemblies.

---

## 2. Agent Workflow (Never Skip)

1. **Ingest Context**
    - Read this playbook, `CLAUDE.md`, and `.github/copilot-instructions.md`.
    - Inspect relevant files before modifying (they may have changed since last view).

2. **Plan**
    - Break the request into actionable steps and manage them with the todo tool when multi-step.
    - Identify which projects/assets are affected (Core, SqlClient, Python wrapper, examples, docs, CI).

3. **Execute**
    - Apply minimal diffs; respect existing style and patterns.
    - Keep C# and Python logic in sync (each public API needs parity).
    - Follow the guardrails in section 4.

4. **Validate**
    - Run the smallest relevant build/test commands (section 6) after edits.
    - Fix failures immediately; never leave the repo broken.

5. **Report**
    - Summarize what changed, how it was validated, and any caveats or follow-ups.

---

## 3. Architecture Overview

- **Single Source of Truth** — HTTP clients, auth, retry logic, serialization all live in C# (`SoftSense.Databricks.Core`); `SoftSense.Databricks.SqlClient` hosts SQL Warehouse-specific APIs; the Python package only wraps the DLLs via pythonnet.
- **Performance-First** — Streaming APIs must avoid buffering or re-serializing entire payloads and should rely on `Utf8JsonReader` + `HttpCompletionOption.ResponseHeadersRead`.
- **AOT Compatibility** — Avoid reflection-based serialization; rely on attributes + explicit writers.
- **Authentication** — Prefer Azure Entra ID via `TokenCredential`, allow PATs via `AccessToken`, and validate configs so only one mode is active.

### Package Map

| Package | Purpose | Notes |
| --- | --- | --- |
| `SoftSense.Databricks.Core` | Shared config, HTTP client, auth, exceptions | Dependency-light; used by everything |
| `SoftSense.Databricks.SqlClient` | SQL Warehouse models + `SqlWarehouseClient` | Streaming + query APIs live here |
| `SoftSense.Databricks.Tests` | xUnit coverage for config/auth/retry/HTTP | Keep fast & deterministic |
| `softsense-databricks-sqlclient` | Python bridge calling .NET DLLs | Located at `source/python/...` |
| `examples/` | Sample apps (C#, Python, Aspire host) | Must stay runnable with minimal setup |

---

## 4. Coding Guardrails

### C-sharp

- **CRITICAL**: ALL variables, parameters, methods, properties, classes, and fields MUST have explicit access modifiers (`public`, `private`, `internal`, `protected`, `private protected`, `protected internal`). NEVER rely on implicit/default modifiers.
- Use `required` for mandatory properties and `sealed` classes by default.
- Async method names end with `Async`; async enumerables must include `[EnumeratorCancellation]`.
- All public members need XML docs.
- Nullable reference types remain enabled; treat warnings as errors.
- JSON serialization: annotate DTOs with `[JsonPropertyName("snake_case")]` and use `Utf8JsonReader/Writer`; never round-trip with `JsonDocument` for streaming scenarios.
- Streaming APIs: request `HttpCompletionOption.ResponseHeadersRead` and yield rows as they arrive; never buffer unless explicitly required.
- **NEVER** use FluentAssertions library; use standard xUnit assertions (`Assert.NotNull`, `Assert.Equal`, `Assert.Throws`, etc.).

### Python

- All functions must have type hints and snake_case naming.
- Keep full parity with C# models/methods (no drifting signatures).
- Provide context managers (`__enter__`/`__exit__`) for disposable .NET clients.
- Enforce `mypy --strict`, `ruff`, `black`; resolve violations rather than ignoring them.
- Call .NET async methods with `.GetAwaiter().GetResult()` (pythonnet constraint) and document sync boundaries.

### Cross-Cutting DO / DON'T

| DO | DON'T |
| --- | --- |
| Use explicit access modifiers on ALL code elements. | Use implicit/default modifiers (e.g., omitting `private`). |
| Use standard xUnit assertions in tests. | Use FluentAssertions library. |
| Validate configs before use (`DatabricksConfig.Validate()`). | Duplicate option objects instead of reusing shared config classes. |
| Keep streaming paths allocation-free. | Deserialize + reserialize JSON for streamed output. |
| Add tests whenever public behavior changes. | Merge without running the relevant build/tests. |
| Update documentation/examples alongside code changes. | Leave the Python wrapper inconsistent with C# APIs. |

---

## 5. Frequently Needed Knowledge

### Query Surface (C#)

```csharp
ExecuteQueryAsync();              // QueryResult
ExecuteQueryJsonAsync();          // JSON array of objects
ExecuteQueryStreamAsync();        // Stream raw JSON arrays
ExecuteQueryStreamNdjsonAsync();  // Stream NDJSON with column names
```

### Python Interop Tips

```python
# Assembly loading
#   Dev:  source/python/... references ../../csharp/**/bin/Debug/net10.0
#   Prod: importlib.resources.files("softsense_databricks_sqlclient").joinpath("lib")

# Calling async .NET methods
dotnet_method(...).GetAwaiter().GetResult()
```

### Configuration Matrix

| Property | Type | Default | Notes |
| --- | --- | --- | --- |
| `WorkspaceUrl` | string | – | Required: [https://workspace-name.databricks.com](https://workspace-name.databricks.com) |
| `WarehouseId` | string | null | Optional default warehouse |
| `Credential` | TokenCredential | null | Azure Entra (preferred) |
| `AccessToken` | string | null | PAT (legacy/CI) |
| `TimeoutSeconds` | int | 300 | HTTP timeout |
| `MaxRetries` | int | 3 | Exponential retry attempts |
| `PollingIntervalMilliseconds` | int | 1000 | Statement status polling |

---

## 6. Build & Test Matrix

```bash
# C#
cd source/csharp
dotnet build
dotnet test
# optional: dotnet format

# Python
cd source/python/softsense-databricks-sqlclient
pip install -e ".[dev]"
pytest tests/
ruff check src/
black src/

```

CI Workflows:

- `.github/workflows/ci.yml` orchestrates.
- `.github/workflows/dotnet-build.yml` triggers on `source/csharp/**` and publishes to GitHub Packages.
- `.github/workflows/python-build.yml` triggers on `source/python/**` (and C# because DLLs matter) and publishes to PyPI.

> **Rule of Thumb:** Touching both languages means running both build pipelines locally (or at least their fast subsets) before opening a PR.

---

## 7. Common Task Recipes

### Adding a New Query Method

1. Implement in `SoftSense.Databricks.SqlClient/SqlWarehouseClient.cs` using the streaming + JSON guardrails.
2. Add DTOs under `SoftSense.Databricks.SqlClient/Models/` with `[JsonPropertyName]`.
3. Document the method (XML docs + README/example snippet).
4. Mirror the API in `softsense_databricks_sqlclient/client.py` with identical semantics.
5. Add tests in `SoftSense.Databricks.Tests` and Python `tests/`.
6. Update samples (`examples/`) and docs as needed.

### Adding a New Service Client (e.g., Unity Catalog)

1. Create `SoftSense.Databricks.{Service}/` project and `.csproj`.
2. Define request/response models inside `Models/`.
3. Write `{Service}Client.cs` with proper auth + retry integration.
4. Cover it in `SoftSense.Databricks.Tests`.
5. Add Python wrappers + tests.
6. Document the API in READMEs/examples.

### Updating Python Wrapper Assemblies

- Ensure `source/csharp` builds produce DLLs that are packaged under `source/python/.../lib/` before publishing.
- Tests must validate assembly loading for both dev (relative paths) and release (packaged resources).

---

## 8. Known Limitations & Future Work

1. Python async story blocks on `.GetAwaiter().GetResult()`; true async interop is future work.
2. Python package bundles .NET DLLs under `lib/`; any new C# dependency must be copied there.
3. Deployments require .NET 10 runtime available wherever the Python package runs.
4. Planned APIs: Unity Catalog, Workspace, Jobs, Clusters.

Planned package map:

- `SoftSense.Databricks.UnityCatalog` - Catalog operations
- `SoftSense.Databricks.Workspace` - Notebooks, repos
- `SoftSense.Databricks.Jobs` - Job management
- `SoftSense.Databricks.Clusters` - Cluster lifecycle

---

## 9. Support & Documentation Map

- Main README: `/README.md`
- Python README: `/source/python/softsense-databricks-sqlclient/README.md`
- Docs portal: `/docs/`
- Examples: `/examples/`
- Issues/Support: GitHub Issues on the main repo

When in doubt, trace existing patterns before inventing new ones. Consistency, performance, and parity between C# and Python are the north stars for every change.
