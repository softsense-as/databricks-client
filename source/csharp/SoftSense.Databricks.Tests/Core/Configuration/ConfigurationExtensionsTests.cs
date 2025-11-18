using FluentAssertions;
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
        configuration.Should().NotBeNull();
        configuration.Should().BeAssignableTo<IConfiguration>();
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
        configuration.Should().NotBeNull();
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
            configuration.Should().NotBeNull();
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
        config.Should().NotBeNull();
        config.WorkspaceUrl.Should().Be("https://test.databricks.com");
        config.AccessToken.Should().Be("test-token");
        config.WarehouseId.Should().Be("test-warehouse-id");
        config.TimeoutSeconds.Should().Be(300);
        config.MaxRetries.Should().Be(3);
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
        config.Should().NotBeNull();
        config.WorkspaceUrl.Should().Be("https://test.databricks.com");
        config.AccessToken.Should().Be("test-token");
        config.WarehouseId.Should().BeNull();
    }

    [Fact]
    public void GetValidatedSection_WithMissingSection_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var act = () => configuration.GetValidatedSection<DatabricksConfig>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Missing DatabricksConfig configuration section.");
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

        // Act
        var act = () => configuration.GetValidatedSection<DatabricksConfig>();

        // Assert - should throw because WorkspaceUrl is required
        // The exception is wrapped in TargetInvocationException due to reflection
        act.Should().Throw<Exception>()
            .Where(ex => ex is ArgumentException || 
                        (ex is System.Reflection.TargetInvocationException && 
                         ex.InnerException is ArgumentException));
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
        config.Should().NotBeNull();
        config.WorkspaceUrl.Should().Be("https://custom.databricks.com");
        config.AccessToken.Should().Be("custom-token");
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
            opt.WarehouseId.Should().Be("test-warehouse-id");
        });

        // Assert
        config.Should().NotBeNull();
        validatorExecuted.Should().BeTrue();
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

        // Act
        var act = () => configuration.GetValidatedSection<DatabricksConfig>(opt =>
        {
            throw new InvalidOperationException("Custom validation failed");
        });

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Custom validation failed");
    }
}
