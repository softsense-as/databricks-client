using SoftSense.Databricks.Core.Exceptions;

namespace SoftSense.Databricks.Tests.Core.Exceptions;

public class DatabricksExceptionTests
{
    [Fact]
    public void DatabricksException_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Test error";

        // Act
        var exception = new DatabricksException(message);

    // Assert
    Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void DatabricksException_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        const string message = "Test error";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new DatabricksException(message, innerException);

    // Assert
    Assert.Equal(message, exception.Message);
    Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void DatabricksAuthenticationException_ShouldInheritFromDatabricksException()
    {
        // Arrange & Act
        var exception = new DatabricksAuthenticationException("Auth failed");

    // Assert
    Assert.IsAssignableFrom<DatabricksException>(exception);
    }

    [Fact]
    public void DatabricksHttpException_ShouldSetStatusCodeAndBody()
    {
        // Arrange
        const int statusCode = 404;
        const string message = "Not found";
        const string body = "{\"error\": \"Resource not found\"}";

        // Act
        var exception = new DatabricksHttpException(statusCode, message, body);

    // Assert
    Assert.Equal(statusCode, exception.StatusCode);
    Assert.Equal(message, exception.Message);
    Assert.Equal(body, exception.ResponseBody);
    Assert.IsAssignableFrom<DatabricksException>(exception);
    }

    [Fact]
    public void DatabricksRateLimitException_ShouldInheritFromHttpException()
    {
        // Arrange
        const int statusCode = 429;
        const string message = "Rate limit exceeded";

        // Act
        var exception = new DatabricksRateLimitException(statusCode, message);

    // Assert
    Assert.Equal(429, exception.StatusCode);
    Assert.IsAssignableFrom<DatabricksHttpException>(exception);
    Assert.IsAssignableFrom<DatabricksException>(exception);
    }
}
