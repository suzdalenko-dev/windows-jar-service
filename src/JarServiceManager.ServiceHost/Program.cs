using JarServiceManager.Core.Configuration;
using JarServiceManager.ServiceHost;

const string ValidateConfigurationCommand = "--validate-config";
const string RunConfigurationCommand      = "--run-config";

if (args.Length != 2)
{
    PrintUsage();
    Environment.ExitCode = 64;
    return;
}

string command = args[0];
string configurationPath = args[1];

bool isValidateCommand = string.Equals(
    command,
    ValidateConfigurationCommand,
    StringComparison.OrdinalIgnoreCase
);

bool isRunCommand = string.Equals(
    command,
    RunConfigurationCommand,
    StringComparison.OrdinalIgnoreCase
);

if (!isValidateCommand && !isRunCommand)
{
    Console.Error.WriteLine(
        $"Unknown command: {command}"
    );

    PrintUsage();
    Environment.ExitCode = 64;
    return;
}

var (configuration, exitCode) =
    await TryLoadValidatedConfigurationAsync(
        configurationPath
    );

if (configuration is null)
{
    Environment.ExitCode = exitCode;
    return;
}

PrintConfigurationSummary(configuration);

if (isValidateCommand)
{
    Environment.ExitCode = 0;
    return;
}

var builder = Host.CreateApplicationBuilder();

builder.Services.AddSingleton<ServiceConfiguration>(
    configuration
);

builder.Services.AddSingleton<JavaProcessRunner>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();

static async Task<(
    ServiceConfiguration? Configuration,
    int ExitCode
)> TryLoadValidatedConfigurationAsync(
    string configurationPath
)
{
    try
    {
        ServiceConfiguration configuration =
            await ServiceConfigurationLoader.LoadAsync(
                configurationPath
            );

        IReadOnlyList<ConfigurationValidationError> errors =
            ServiceConfigurationValidator.Validate(
                configuration
            );

        if (errors.Count == 0)
        {
            return (configuration, 0);
        }

        Console.Error.WriteLine(
            $"Configuration contains {errors.Count} error(s):"
        );

        foreach (ConfigurationValidationError error in errors)
        {
            Console.Error.WriteLine(
                $"- {error.PropertyPath}: {error.Message}"
            );
        }

        return (null, 2);
    }
    catch (ServiceConfigurationLoadException exception)
    {
        Console.Error.WriteLine(exception.Message);
        return (null, 1);
    }
}

static void PrintConfigurationSummary(
    ServiceConfiguration configuration
)
{
    Console.WriteLine("Configuration is valid.");

    Console.WriteLine(
        $"Service: {configuration.Service.Id}"
    );

    Console.WriteLine(
        $"Java:    {configuration.Java.ExecutablePath}"
    );

    Console.WriteLine(
        $"JAR:     {configuration.Application.JarPath}"
    );
}

static void PrintUsage()
{
    Console.Error.WriteLine("Usage:");

    Console.Error.WriteLine(
        "  ServiceHost --validate-config <configuration-path>"
    );

    Console.Error.WriteLine(
        "  ServiceHost --run-config <configuration-path>"
    );
}