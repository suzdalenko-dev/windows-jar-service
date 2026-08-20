using JarServiceManager.Core.Configuration;

namespace JarServiceManager.ServiceHost;

public sealed class Worker(ILogger<Worker> logger, JavaProcessRunner javaProcessRunner, ServiceConfiguration configuration, IHostApplicationLifetime applicationLifetime): BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            int exitCode = await javaProcessRunner.RunAsync(
                configuration,
                stoppingToken
            );

            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            Environment.ExitCode = exitCode;

            logger.LogWarning(
                "The Java process finished. " +
                "The ServiceHost will stop with exit code {ExitCode}.",
                exitCode
            );

            applicationLifetime.StopApplication();
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Java process supervision was cancelled."
            );
        }
        catch (Exception exception)
        {
            Environment.ExitCode = 3;

            logger.LogCritical(
                exception,
                "Java process supervision failed."
            );

            applicationLifetime.StopApplication();
        }
    }
}