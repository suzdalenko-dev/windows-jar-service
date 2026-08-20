namespace JarServiceManager.Core.Configuration;

public static class ServiceConfigurationValidator
{
    public static IReadOnlyList<ConfigurationValidationError> Validate(
        ServiceConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var errors = new List<ConfigurationValidationError>();

        if (configuration.SchemaVersion != 1)
        {
            Add(
                errors,
                "schemaVersion",
                $"Unsupported schema version: {configuration.SchemaVersion}."
            );
        }

        ValidateRequired(
            configuration.Service.Id,
            "service.id",
            errors
        );

        if (!string.IsNullOrWhiteSpace(configuration.Service.Id) &&
            configuration.Service.Id.Any(
                character =>
                    !char.IsLetterOrDigit(character) &&
                    character is not '-' and not '_' and not '.'))
        {
            Add(
                errors,
                "service.id",
                "Only letters, numbers, hyphens, underscores and dots are allowed."
            );
        }

        ValidateRequired(
            configuration.Service.DisplayName,
            "service.displayName",
            errors
        );

        ValidateExistingFile(
            configuration.Java.ExecutablePath,
            "java.executablePath",
            ".exe",
            "java.exe",
            errors
        );

        ValidateExistingFile(
            configuration.Application.JarPath,
            "application.jarPath",
            ".jar",
            requiredFileName: null,
            errors
        );

        ValidateExistingDirectory(
            configuration.Application.WorkingDirectory,
            "application.workingDirectory",
            errors
        );

        ValidateRange(
            configuration.Timeouts.StartupSeconds,
            1,
            3600,
            "timeouts.startupSeconds",
            errors
        );

        ValidateRange(
            configuration.Timeouts.ShutdownSeconds,
            1,
            3600,
            "timeouts.shutdownSeconds",
            errors
        );

        ValidateRange(
            configuration.Restart.MaximumAttempts,
            1,
            100,
            "restart.maximumAttempts",
            errors
        );

        ValidateRange(
            configuration.Restart.ResetAfterMinutes,
            1,
            1440,
            "restart.resetAfterMinutes",
            errors
        );

        ValidateRange(
            configuration.Restart.InitialDelaySeconds,
            0,
            3600,
            "restart.initialDelaySeconds",
            errors
        );

        ValidateRange(
            configuration.Restart.MaximumDelaySeconds,
            0,
            86400,
            "restart.maximumDelaySeconds",
            errors
        );

        if (configuration.Restart.MaximumDelaySeconds <
            configuration.Restart.InitialDelaySeconds)
        {
            Add(
                errors,
                "restart.maximumDelaySeconds",
                "The maximum delay cannot be smaller than the initial delay."
            );
        }

        ValidateHealthCheck(configuration.HealthCheck, errors);

        ValidateAbsolutePath(
            configuration.Logs.Directory,
            "logs.directory",
            errors
        );

        ValidateRange(
            configuration.Logs.MaximumFileSizeMegabytes,
            1,
            1024,
            "logs.maximumFileSizeMegabytes",
            errors
        );

        ValidateRange(
            configuration.Logs.RetainedFileCount,
            1,
            1000,
            "logs.retainedFileCount",
            errors
        );

        return errors;
    }

    private static void ValidateHealthCheck(
        HealthCheckSettings healthCheck,
        List<ConfigurationValidationError> errors)
    {
        ValidateRange(
            healthCheck.IntervalSeconds,
            1,
            86400,
            "healthCheck.intervalSeconds",
            errors
        );

        ValidateRange(
            healthCheck.TimeoutSeconds,
            1,
            3600,
            "healthCheck.timeoutSeconds",
            errors
        );

        ValidateRange(
            healthCheck.FailureThreshold,
            1,
            100,
            "healthCheck.failureThreshold",
            errors
        );

        ValidateRange(
            healthCheck.ExpectedStatusCode,
            100,
            599,
            "healthCheck.expectedStatusCode",
            errors
        );

        if (!healthCheck.Enabled)
        {
            return;
        }

        if (healthCheck.Type is
            HealthCheckType.HttpStatus or HealthCheckType.HttpJson)
        {
            if (!Uri.TryCreate(
                    healthCheck.Url,
                    UriKind.Absolute,
                    out var healthCheckUri) ||
                healthCheckUri.Scheme is not "http" and not "https")
            {
                Add(
                    errors,
                    "healthCheck.url",
                    "A valid absolute HTTP or HTTPS URL is required."
                );
            }
        }

        if (healthCheck.Type == HealthCheckType.HttpJson)
        {
            ValidateRequired(
                healthCheck.ExpectedJsonProperty,
                "healthCheck.expectedJsonProperty",
                errors
            );

            ValidateRequired(
                healthCheck.ExpectedJsonValue,
                "healthCheck.expectedJsonValue",
                errors
            );
        }
    }

    private static void ValidateExistingFile(
        string value,
        string propertyPath,
        string requiredExtension,
        string? requiredFileName,
        List<ConfigurationValidationError> errors)
    {
        if (!TryGetFullPath(value, propertyPath, errors, out var fullPath))
        {
            return;
        }

        if (!File.Exists(fullPath))
        {
            Add(errors, propertyPath, $"The file does not exist: {fullPath}");
            return;
        }

        if (!string.Equals(
                Path.GetExtension(fullPath),
                requiredExtension,
                StringComparison.OrdinalIgnoreCase))
        {
            Add(
                errors,
                propertyPath,
                $"The file must have the {requiredExtension} extension."
            );
        }

        if (requiredFileName is not null &&
            !string.Equals(
                Path.GetFileName(fullPath),
                requiredFileName,
                StringComparison.OrdinalIgnoreCase))
        {
            Add(
                errors,
                propertyPath,
                $"The executable must be named {requiredFileName}."
            );
        }
    }

    private static void ValidateExistingDirectory(
        string value,
        string propertyPath,
        List<ConfigurationValidationError> errors)
    {
        if (!TryGetFullPath(value, propertyPath, errors, out var fullPath))
        {
            return;
        }

        if (!Directory.Exists(fullPath))
        {
            Add(
                errors,
                propertyPath,
                $"The directory does not exist: {fullPath}"
            );
        }
    }

    private static void ValidateAbsolutePath(
        string value,
        string propertyPath,
        List<ConfigurationValidationError> errors)
    {
        TryGetFullPath(value, propertyPath, errors, out _);
    }

    private static bool TryGetFullPath(
        string value,
        string propertyPath,
        List<ConfigurationValidationError> errors,
        out string fullPath)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, propertyPath, "A path is required.");
            return false;
        }

        try
        {
            if (!Path.IsPathFullyQualified(value))
            {
                Add(errors, propertyPath, "The path must be absolute.");
                return false;
            }

            fullPath = Path.GetFullPath(value);
            return true;
        }
        catch (Exception exception)
            when (exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            Add(errors, propertyPath, "The path is invalid.");
            return false;
        }
    }

    private static void ValidateRequired(
        string value,
        string propertyPath,
        List<ConfigurationValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, propertyPath, "A value is required.");
        }
    }

    private static void ValidateRange(
        int value,
        int minimum,
        int maximum,
        string propertyPath,
        List<ConfigurationValidationError> errors)
    {
        if (value < minimum || value > maximum)
        {
            Add(
                errors,
                propertyPath,
                $"The value must be between {minimum} and {maximum}."
            );
        }
    }

    private static void Add(
        List<ConfigurationValidationError> errors,
        string propertyPath,
        string message)
    {
        errors.Add(new ConfigurationValidationError(propertyPath, message));
    }
}

public sealed record ConfigurationValidationError(
    string PropertyPath,
    string Message
);