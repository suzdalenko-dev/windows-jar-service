using JarServiceManager.Core.Configuration;
using JarServiceManager.ServiceHost;

if (args.Length > 0 &&
    string.Equals(
        args[0],
        "--validate-config",
        StringComparison.OrdinalIgnoreCase))
{
    var configurationPath =
        args.Length > 1
            ? args[1]
            : Path.Combine("config", "service.json");

    Environment.ExitCode =
        await ValidateConfigurationAsync(configurationPath);

    return;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
var host = builder.Build();
host.Run();

static async Task<int> ValidateConfigurationAsync(string configurationPath)
{
    try
    {
        var configuration =
            await ServiceConfigurationLoader.LoadAsync(configurationPath);

        var errors =
            ServiceConfigurationValidator.Validate(configuration);

        if (errors.Count > 0)
        {
            Console.Error.WriteLine(
                $"Configuration contains {errors.Count} error(s):"
            );

            foreach (var error in errors)
            {
                Console.Error.WriteLine(
                    $"- {error.PropertyPath}: {error.Message}"
                );
            }

            return 2;
        }

        Console.WriteLine("Configuration is valid.");
        Console.WriteLine($"Service: {configuration.Service.Id}");
        Console.WriteLine($"Java:    {configuration.Java.ExecutablePath}");
        Console.WriteLine($"JAR:     {configuration.Application.JarPath}");

        return 0;
    }
    catch (ServiceConfigurationLoadException exception)
    {
        Console.Error.WriteLine(exception.Message);
        return 1;
    }
}