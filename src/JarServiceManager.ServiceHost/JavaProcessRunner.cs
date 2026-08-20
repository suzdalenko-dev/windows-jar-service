using System.ComponentModel;
using System.Diagnostics;

using JarServiceManager.Core.Configuration;

namespace JarServiceManager.ServiceHost;

public sealed class JavaProcessRunner(ILogger<JavaProcessRunner> logger)
{
    public async Task<int> RunAsync(ServiceConfiguration configuration, CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var startInfo = CreateStartInfo(configuration);

        using var process = new Process
        {
            StartInfo = startInfo
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "The Java process could not be started."
                );
            }
        }
        catch (Exception exception)
            when (exception is Win32Exception
                or InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Java could not be started: {configuration.Java.ExecutablePath}",
                exception
            );
        }

        logger.LogInformation(
            "Java process started. PID: {ProcessId}. JAR: {JarPath}",
            process.Id,
            configuration.Application.JarPath
        );

        Task standardOutputTask = ForwardOutputAsync(
            process.StandardOutput,
            LogLevel.Information
        );

        Task standardErrorTask = ForwardOutputAsync(
            process.StandardError,
            LogLevel.Error
        );

        try
        {
            await process.WaitForExitAsync(stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            await StopProcessTreeAsync(process);

            await Task.WhenAll(
                standardOutputTask,
                standardErrorTask
            );

            throw;
        }

        await Task.WhenAll(
            standardOutputTask,
            standardErrorTask
        );

        int exitCode = process.ExitCode;

        logger.LogInformation(
            "Java process exited with code {ExitCode}.",
            exitCode
        );

        return exitCode;
    }

    private static ProcessStartInfo CreateStartInfo(
        ServiceConfiguration configuration
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = configuration.Java.ExecutablePath,
            WorkingDirectory =
                configuration.Application.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            CreateNoWindow = true
        };

        foreach (string argument in configuration.Java.JvmArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add("-jar");
        startInfo.ArgumentList.Add(configuration.Application.JarPath);

        foreach (string argument in configuration.Application.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (
            KeyValuePair<string, string> variable
            in configuration.Application.EnvironmentVariables
        )
        {
            startInfo.Environment[variable.Key] = variable.Value;
        }

        return startInfo;
    }

    private async Task ForwardOutputAsync(
        StreamReader reader,
        LogLevel logLevel
    )
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            logger.Log(
                logLevel,
                "[java] {Line}",
                line
            );
        }
    }

    private async Task StopProcessTreeAsync(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        logger.LogWarning(
            "Stopping Java process tree. PID: {ProcessId}.",
            process.Id
        );

        try
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
        catch (InvalidOperationException)
        {
            if (!process.HasExited)
            {
                throw;
            }
        }

        logger.LogInformation(
            "Java process tree has been terminated."
        );
    }
}