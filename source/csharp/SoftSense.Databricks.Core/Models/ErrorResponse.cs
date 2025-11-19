using System.Text.Json.Serialization;

namespace SoftSense.Databricks.Core.Models;

/// <summary>
/// Standard error response from Databricks API
/// </summary>
public sealed class ErrorResponse
{
    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("details")]
    public object? Details { get; set; }

    public override string ToString()
    {
        return $"Error {ErrorCode}: {Message}";
    }
}
