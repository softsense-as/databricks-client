using SoftSense.Databricks.Core.Configuration;

namespace SoftSense.Databricks.Tests.Core.Configuration;

public class DatabricksConfigTests
{
    [Fact]
    public void Validate_WithValidPatConfig_ShouldNotThrow()
    {
        // Arrange
        var config = new DatabricksConfig
        {
            WorkspaceUrl = "https://test.databricks.com",
            AccessToken = "test-token"
        };

    // Act
    var exception = Record.Exception(() => config.Validate());

    // Assert
    Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithMissingWorkspaceUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new DatabricksConfig
        {
            WorkspaceUrl = "",
            AccessToken = "test-token"
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("WorkspaceUrl", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithMissingCredentialAndToken_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new DatabricksConfig
        {
            WorkspaceUrl = "https://test.databricks.com",
            Credential = null,
            AccessToken = null
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("Credential", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithInvalidWorkspaceUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new DatabricksConfig
        {
            WorkspaceUrl = "not-a-url",
            AccessToken = "test-token"
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("valid URL", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidTimeoutSeconds_ShouldThrowArgumentException(int timeout)
    {
        // Arrange
        var config = new DatabricksConfig
        {
            WorkspaceUrl = "https://test.databricks.com",
            AccessToken = "test-token",
            TimeoutSeconds = timeout
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("TimeoutSeconds", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithNegativeMaxRetries_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new DatabricksConfig
        {
            WorkspaceUrl = "https://test.databricks.com",
            AccessToken = "test-token",
            MaxRetries = -1
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("MaxRetries", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidPollingInterval_ShouldThrowArgumentException(int interval)
    {
        // Arrange
        var config = new DatabricksConfig
        {
            WorkspaceUrl = "https://test.databricks.com",
            AccessToken = "test-token",
            PollingIntervalMilliseconds = interval
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.Validate());
        Assert.Contains("PollingIntervalMilliseconds", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var config = new DatabricksConfig
        {
            WorkspaceUrl = "https://test.databricks.com",
            AccessToken = "test-token"
        };

        // Assert
    Assert.Equal(300, config.TimeoutSeconds);
    Assert.Equal(3, config.MaxRetries);
    Assert.Equal(1000, config.PollingIntervalMilliseconds);
    Assert.Equal("2ff814a6-3304-4ab8-85cb-cd0e6f879c1d", config.AzureResourceId);
    }
}
