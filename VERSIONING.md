# Package Versioning

Packages are versioned using semantic versioning with git commit short hash suffix.

## Version Format

```
{major}.{minor}.{patch}-{short-hash}
```

Example: `0.0.1-a1b2c3d`

## Initial Versions

All first builds will use `0.0.x-{hash}` format:
- `0.0.1-a1b2c3d` - First build
- `0.0.2-b2c3d4e` - Second build
- etc.

## Setting Version

### Automatic (Push/PR)
Builds on push or PR automatically use `0.0.1-{hash}`:
```bash
git push origin main
# Results in: 0.0.1-a1b2c3d
```

### Manual (Workflow Dispatch)
Manually trigger with custom version:
```bash
# GitHub UI: Actions → .NET Build → Run workflow
# Input version: 0.0.2
# Results in: 0.0.2-a1b2c3d
```

### Release
On GitHub release, version from input is used:
```bash
# Create release v0.1.0
# Results in: 0.1.0-a1b2c3d
```

## Package Locations

### GitHub Packages (NuGet)
- Published on every release
- URL: `https://nuget.pkg.github.com/softsense/index.json`

### NuGet.org
- Currently disabled (commented out)
- Uncomment `publish-nuget` job in `.github/workflows/dotnet-build.yml` to enable

## Local Development

Local builds default to `0.0.1-local`:
```bash
dotnet build
# Results in: 0.0.1-local
```

Override version:
```bash
dotnet build /p:Version=0.0.5-test
# Results in: 0.0.5-test
```

## Implementation Details

### Project Files
Both `.csproj` files use conditional version:
```xml
<Version Condition="'$(Version)' == ''">0.0.1-local</Version>
```

### GitHub Workflow
`.github/workflows/dotnet-build.yml` generates version:
```yaml
- name: Generate version
  run: |
    VERSION="0.0.1"  # or from workflow input
    SHORT_HASH=$(git rev-parse --short HEAD)
    FULL_VERSION="${VERSION}-${SHORT_HASH}"
    echo "version=${FULL_VERSION}" >> $GITHUB_OUTPUT
```

## Consumer Usage

### GitHub Packages
Add to `NuGet.config`:
```xml
<packageSources>
  <add key="github" value="https://nuget.pkg.github.com/softsense/index.json" />
</packageSources>
```

Install:
```bash
dotnet add package SoftSense.Databricks.Core --version 0.0.1-a1b2c3d
dotnet add package SoftSense.Databricks.SqlClient --version 0.0.1-a1b2c3d
```

### Wildcard Version (Latest Prerelease)
```bash
dotnet add package SoftSense.Databricks.Core --prerelease
```
