namespace SoftSense.Databricks.Core.Configuration;

/// <summary>
/// Warehouse-specific options for SQL queries
/// </summary>
/// <remarks>
/// [Obsolete] This class is deprecated. Use DatabricksConfig.WarehouseId, DatabricksConfig.Catalog, 
/// and DatabricksConfig.Schema properties instead. This class will be removed in a future version.
/// </remarks>
[Obsolete("Use DatabricksConfig.WarehouseId, DatabricksConfig.Catalog, and DatabricksConfig.Schema instead. This class will be removed in a future version.")]
public sealed class WarehouseOptions
{
    public required string  WarehouseId { get; init; }
    public          string? Catalog     { get; init; }
    public          string? Schema      { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(WarehouseId))
        {
            throw new InvalidOperationException("Warehouse:WarehouseId is required.");
        }
    }
}
