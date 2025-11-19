using Microsoft.Extensions.Configuration;
using SoftSense.Databricks.Core.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var databricksConfig = builder.Configuration
    .GetSection(nameof(DatabricksConfig))
    .Get<DatabricksConfig>()
    ?? throw new InvalidOperationException("Missing Databricks configuration. Please configure the 'DatabricksConfig' section in appsettings or user secrets.");

databricksConfig.Validate();

if (string.IsNullOrWhiteSpace(databricksConfig.WarehouseId))
{
    throw new InvalidOperationException("DatabricksConfig:WarehouseId must be configured for the Aspire AppHost.");
}

var databricksWorkspaceUrl = databricksConfig.WorkspaceUrl;
var databricksWarehouseId = databricksConfig.WarehouseId;
var databricksToken = databricksConfig.AccessToken ?? string.Empty;

// Add .NET Console Example
var dotnetExample = builder.AddProject<Projects.SoftSense_Examples_Console>("dotnet-example")
   .WithEnvironment("DatabricksConfig__WorkspaceUrl", databricksWorkspaceUrl)
   .WithEnvironment("DatabricksConfig__WarehouseId", databricksWarehouseId)
   .WithEnvironment("DatabricksConfig__AccessToken", databricksToken)
   .WithEnvironment("DEMO_MODE", "true") // Run in non-interactive demo mode
   .WithExplicitStart();

// Add Python Console Example using uv
var pythonExample = builder.AddPythonExecutable("python-example",
        "../../examples/python/databricks-example-console",
        "python")
    .WithArgs("-m", "databricks_example.app")
    .WithEnvironment("DatabricksConfig__WorkspaceUrl", databricksWorkspaceUrl)
    .WithEnvironment("DatabricksConfig__WarehouseId", databricksWarehouseId)
    .WithEnvironment("DatabricksConfig__AccessToken", databricksToken)
    .WithEnvironment("DEMO_MODE", "true")  // Run in non-interactive demo mode
    //.WithVirtualEnvironment(".venv")
    .WithUv()  // Use uv for package management
    .WithExplicitStart();

builder.Build().Run();
