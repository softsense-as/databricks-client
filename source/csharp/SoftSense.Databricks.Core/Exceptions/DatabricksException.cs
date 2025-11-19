namespace SoftSense.Databricks.Core.Exceptions;

/// <summary>
/// Base exception for all Databricks-related errors
/// </summary>
public class DatabricksException : Exception
{
    public DatabricksException(string message) : base(message) { }

    public DatabricksException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when authentication fails
/// </summary>
public class DatabricksAuthenticationException : DatabricksException
{
    public DatabricksAuthenticationException(string message) : base(message) { }

    public DatabricksAuthenticationException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when an HTTP request fails
/// </summary>
public class DatabricksHttpException : DatabricksException
{
    public int StatusCode { get; }
    public string? ResponseBody { get; }

    public DatabricksHttpException(int statusCode, string message, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public DatabricksHttpException(int statusCode, string message, string? responseBody, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

/// <summary>
/// Exception thrown when rate limit is exceeded
/// </summary>
public class DatabricksRateLimitException : DatabricksHttpException
{
    public TimeSpan? RetryAfter { get; }

    public DatabricksRateLimitException(int statusCode, string message, TimeSpan? retryAfter = null)
        : base(statusCode, message)
    {
        RetryAfter = retryAfter;
    }
}
