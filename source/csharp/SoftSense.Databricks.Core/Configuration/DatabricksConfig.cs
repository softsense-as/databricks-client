using Azure.Core;

namespace SoftSense.Databricks.Core.Configuration;

/// <summary>
/// Configuration for connecting to Databricks workspace
/// </summary>
public sealed class DatabricksConfig
{
    /// <summary>
    /// Databricks workspace URL (e.g., https://your-workspace.databricks.com)
    /// </summary>
    public required string WorkspaceUrl { get; init; }

    /// <summary>
    /// Azure Entra ID credential for authentication
    /// </summary>
    public TokenCredential? Credential { get; init; }

    /// <summary>
    /// Personal Access Token for authentication
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Default SQL Warehouse ID to use for queries (optional)
    /// </summary>
    public string? WarehouseId { get; init; }
    
    /// <summary>
    /// Azure resource ID for Databricks (default: 2ff814a6-3304-4ab8-85cb-cd0e6f879c1d)
    /// This is the standard Azure Databricks resource ID
    /// </summary>
    public string AzureResourceId { get; init; } = "2ff814a6-3304-4ab8-85cb-cd0e6f879c1d";

    /// <summary>
    /// HTTP timeout in seconds (default: 300)
    /// </summary>
    public int TimeoutSeconds { get; init; } = 300;

    /// <summary>
    /// Maximum number of retry attempts (default: 3)
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Polling interval in milliseconds for query status checks (default: 1000)
    /// </summary>
    public int PollingIntervalMilliseconds { get; init; } = 1000;

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(WorkspaceUrl))
            throw new ArgumentException("WorkspaceUrl is required", nameof(WorkspaceUrl));

        if (Credential is null && string.IsNullOrWhiteSpace(AccessToken))
            throw new ArgumentException(
                "Either Credential (Azure Entra) or AccessToken must be provided",
                nameof(Credential));

        if (!Uri.TryCreate(WorkspaceUrl, UriKind.Absolute, out _))
            throw new ArgumentException("WorkspaceUrl must be a valid URL", nameof(WorkspaceUrl));

        if (TimeoutSeconds <= 0)
            throw new ArgumentException("TimeoutSeconds must be positive", nameof(TimeoutSeconds));

        if (MaxRetries < 0)
            throw new ArgumentException("MaxRetries cannot be negative", nameof(MaxRetries));

        if (PollingIntervalMilliseconds <= 0)
            throw new ArgumentException("PollingIntervalMilliseconds must be positive", nameof(PollingIntervalMilliseconds));
    }
}
