using Microsoft.Extensions.Configuration;
using SoftSense.Databricks.Core.Configuration;

namespace SoftSense.Databricks.Tests.Core.Configuration;

public sealed class ConfigurationExtensionsTests
{
    [Fact]
    public void BuildStandardConfiguration_WithDefaultParameters_ShouldBuildConfiguration()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var configuration = builder.BuildStandardConfiguration();

        // Assert
        Assert.NotNull(configuration);
        Assert.IsAssignableFrom<IConfiguration>(configuration);
    }

    [Fact]
    public void BuildStandardConfiguration_WithCustomBasePath_ShouldUseCustomPath()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var customPath = Path.GetTempPath();

        // Act
        var configuration = builder.BuildStandardConfiguration(basePath: customPath);

        // Assert
        Assert.NotNull(configuration);
    }

    [Fact]
    public void BuildStandardConfiguration_WithCustomEnvironmentVariable_ShouldRespectEnvironmentName()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var customEnvVar = "TEST_ENV";
        var previousValue = Environment.GetEnvironmentVariable(customEnvVar);

        try
        {
            Environment.SetEnvironmentVariable(customEnvVar, "Testing");

            // Act
            var configuration = builder.BuildStandardConfiguration(environmentVariableName: customEnvVar);

            // Assert
            Assert.NotNull(configuration);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(customEnvVar, previousValue);
        }
    }

    [Fact]
    public void GetValidatedSection_WithValidDatabricksConfig_ShouldReturnConfigAndValidate()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabricksConfig:WorkspaceUrl"] = "https://test.databricks.com",
                ["DatabricksConfig:AccessToken"] = "test-token",
                ["DatabricksConfig:WarehouseId"] = "test-warehouse-id",
                ["DatabricksConfig:Catalog"] = "test-catalog",
                ["DatabricksConfig:Schema"] = "test-schema",
                ["DatabricksConfig:TimeoutSeconds"] = "300",
                ["DatabricksConfig:MaxRetries"] = "3"
            })
            .Build();

        // Act
        var config = configuration.GetValidatedSection<DatabricksConfig>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://test.databricks.com", config.WorkspaceUrl);
        Assert.Equal("test-token", config.AccessToken);
        Assert.Equal("test-warehouse-id", config.WarehouseId);
        Assert.Equal(300, config.TimeoutSeconds);
        Assert.Equal(3, config.MaxRetries);
    }

    [Fact]
    public void GetValidatedSection_WithDatabricksConfigWithoutWarehouse_ShouldSucceed()
    {
        // Arrange - Warehouse is optional in DatabricksConfig
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabricksConfig:WorkspaceUrl"] = "https://test.databricks.com",
                ["DatabricksConfig:AccessToken"] = "test-token"
            })
            .Build();

        // Act
        var config = configuration.GetValidatedSection<DatabricksConfig>();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://test.databricks.com", config.WorkspaceUrl);
        Assert.Equal("test-token", config.AccessToken);
        Assert.Null(config.WarehouseId);
    }

    [Fact]
    public void GetValidatedSection_WithMissingSection_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.GetValidatedSection<DatabricksConfig>());
        Assert.Equal("Missing DatabricksConfig configuration section.", exception.Message);
    }

    [Fact]
    public void GetValidatedSection_WithInvalidConfig_ShouldThrowValidationException()
    {
        // Arrange - missing required WorkspaceUrl
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabricksConfig:AccessToken"] = "test-token"
            })
            .Build();

        // Act & Assert - should throw because WorkspaceUrl is required
        Assert.ThrowsAny<Exception>(() => configuration.GetValidatedSection<DatabricksConfig>());
    }

    [Fact]
    public void GetValidatedSection_WithCustomSectionName_ShouldUseCustomName()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CustomSection:WorkspaceUrl"] = "https://custom.databricks.com",
                ["CustomSection:AccessToken"] = "custom-token"
            })
            .Build();

        // Act
        var config = configuration.GetValidatedSection<DatabricksConfig>("CustomSection");

        // Assert
        Assert.NotNull(config);
        Assert.Equal("https://custom.databricks.com", config.WorkspaceUrl);
        Assert.Equal("custom-token", config.AccessToken);
    }

    [Fact]
    public void GetValidatedSection_WithCustomValidator_ShouldExecuteValidator()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabricksConfig:WorkspaceUrl"] = "https://test.databricks.com",
                ["DatabricksConfig:AccessToken"] = "test-token",
                ["DatabricksConfig:WarehouseId"] = "test-warehouse-id"
            })
            .Build();

        var validatorExecuted = false;

        // Act
        var config = configuration.GetValidatedSection<DatabricksConfig>(opt =>
        {
            validatorExecuted = true;
            Assert.Equal("test-warehouse-id", opt.WarehouseId);
        });

        // Assert
        Assert.NotNull(config);
        Assert.True(validatorExecuted);
    }

    [Fact]
    public void GetValidatedSection_WithCustomValidatorThrowingException_ShouldPropagateException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabricksConfig:WorkspaceUrl"] = "https://test.databricks.com",
                ["DatabricksConfig:AccessToken"] = "test-token"
            })
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.GetValidatedSection<DatabricksConfig>(opt =>
            {
                throw new InvalidOperationException("Custom validation failed");
            }));
        Assert.Equal("Custom validation failed", exception.Message);
    }
}
