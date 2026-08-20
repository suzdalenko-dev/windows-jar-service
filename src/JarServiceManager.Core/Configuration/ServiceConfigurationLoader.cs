using System.Text.Json;

namespace JarServiceManager.Core.Configuration;

public static class ServiceConfigurationLoader
{
    public static async Task<ServiceConfiguration> LoadAsync(string configurationPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configurationPath))
        {
            throw new ServiceConfigurationLoadException(
                "The configuration path is required."
            );
        }

        string fullPath;

        try
        {
            fullPath = Path.GetFullPath(configurationPath);
        }
        catch (Exception exception)
            when (exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            throw new ServiceConfigurationLoadException(
                $"The configuration path is invalid: {configurationPath}",
                exception
            );
        }

        if (!File.Exists(fullPath))
        {
            throw new ServiceConfigurationLoadException(
                $"The configuration file does not exist: {fullPath}"
            );
        }

        try
        {
            await using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true
            );

            var configuration =
                await JsonSerializer.DeserializeAsync<ServiceConfiguration>(
                    stream,
                    ServiceConfigurationJson.Options,
                    cancellationToken
                );

            return configuration
                ?? throw new ServiceConfigurationLoadException(
                    $"The configuration file is empty: {fullPath}"
                );
        }
        catch (JsonException exception)
        {
            throw new ServiceConfigurationLoadException(
                $"The configuration file contains invalid JSON: {exception.Message}",
                exception
            );
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new ServiceConfigurationLoadException(
                $"Access to the configuration file was denied: {fullPath}",
                exception
            );
        }
        catch (IOException exception)
        {
            throw new ServiceConfigurationLoadException(
                $"The configuration file could not be read: {fullPath}",
                exception
            );
        }
    }
}

public sealed class ServiceConfigurationLoadException : Exception
{
    public ServiceConfigurationLoadException(string message): base(message)
    {
    }

    public ServiceConfigurationLoadException(string message, Exception innerException): base(message, innerException)
    {
    }
}