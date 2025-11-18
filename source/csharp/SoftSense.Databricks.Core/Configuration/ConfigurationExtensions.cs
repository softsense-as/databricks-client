using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SoftSense.Databricks.Core.Configuration;

/// <summary>
/// Extension methods for configuration binding, validation, and builder setup
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Creates a standard configuration with JSON files and environment-specific settings.
    /// Includes appsettings.json, appsettings.{environment}.json files and any user secrets
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="basePath">The base path for configuration files. If null, uses AppContext.BaseDirectory.</param>
    /// <param name="environmentVariableName">The environment variable name to read for environment-specific settings. Defaults to "DOTNET_ENVIRONMENT".</param>
    /// <returns>The built IConfiguration instance.</returns>
    public static IConfiguration BuildStandardConfiguration(
        this ConfigurationBuilder builder,
        string? basePath = null,
        string environmentVariableName = "DOTNET_ENVIRONMENT")
    {
        var environmentName = Environment.GetEnvironmentVariable(environmentVariableName) ?? "Production";
        var configBasePath = basePath ?? AppContext.BaseDirectory;

        return builder
            .SetBasePath(configBasePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .AddUserSecrets(Assembly.GetEntryAssembly()!, false)
            .Build();
    }

    /// <summary>
    /// Binds a configuration section to a strongly-typed object and validates it.
    /// </summary>
    /// <typeparam name="TOptions">The type of options to bind.</typeparam>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="sectionName">The name of the configuration section to bind. If null, uses the type name.</param>
    /// <returns>The bound and validated options instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the configuration section is missing or validation fails.</exception>
    public static TOptions GetValidatedSection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(
        this IConfiguration configuration,
        string? sectionName = null)
        where TOptions : class
    {
        var section = sectionName ?? typeof(TOptions).Name;
        
        var options = configuration
            .GetSection(section)
            .Get<TOptions>()
            ?? throw new InvalidOperationException($"Missing {section} configuration section.");

        // Check if the type has a Validate method and call it
        var validateMethod = typeof(TOptions).GetMethod("Validate", Type.EmptyTypes);
        if (validateMethod is not null)
        {
            validateMethod.Invoke(options, null);
        }

        return options;
    }

    /// <summary>
    /// Binds a configuration section to a strongly-typed object and validates it using a custom validator.
    /// </summary>
    /// <typeparam name="TOptions">The type of options to bind.</typeparam>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="validator">A custom validation action to execute after binding.</param>
    /// <param name="sectionName">The name of the configuration section to bind. If null, uses the type name.</param>
    /// <returns>The bound and validated options instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the configuration section is missing.</exception>
    public static TOptions GetValidatedSection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(
        this IConfiguration configuration,
        Action<TOptions> validator,
        string? sectionName = null)
        where TOptions : class
    {
        var section = sectionName ?? typeof(TOptions).Name;
        
        var options = configuration
            .GetSection(section)
            .Get<TOptions>()
            ?? throw new InvalidOperationException($"Missing {section} configuration section.");

        validator(options);

        return options;
    }
}
