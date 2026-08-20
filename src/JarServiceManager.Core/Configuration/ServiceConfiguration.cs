namespace JarServiceManager.Core.Configuration;

public sealed class ServiceConfiguration
{
    public int SchemaVersion { get; set; } = 1;

    public ServiceSettings Service { get; set; } = new();

    public JavaSettings Java { get; set; } = new();

    public JarApplicationSettings Application { get; set; } = new();

    public TimeoutSettings Timeouts { get; set; } = new();

    public RestartSettings Restart { get; set; } = new();

    public HealthCheckSettings HealthCheck { get; set; } = new();

    public LogSettings Logs { get; set; } = new();
}

public sealed class ServiceSettings
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } =
        "Runs a Java JAR as a Windows service.";

    public ServiceStartMode StartMode { get; set; } =
        ServiceStartMode.AutomaticDelayedStart;
}

public sealed class JavaSettings
{
    public string ExecutablePath { get; set; } = string.Empty;

    public List<string> JvmArguments { get; set; } = [];
}

public sealed class JarApplicationSettings
{
    public string JarPath { get; set; } = string.Empty;

    public string WorkingDirectory { get; set; } = string.Empty;

    public List<string> Arguments { get; set; } = [];

    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];
}

public sealed class TimeoutSettings
{
    public int StartupSeconds { get; set; } = 120;

    public int ShutdownSeconds { get; set; } = 30;

    public bool ForceKillAfterShutdownTimeout { get; set; } = true;
}

public sealed class RestartSettings
{
    public RestartPolicy Policy { get; set; } = RestartPolicy.OnFailure;

    public int MaximumAttempts { get; set; } = 5;

    public int ResetAfterMinutes { get; set; } = 30;

    public int InitialDelaySeconds { get; set; } = 10;

    public int MaximumDelaySeconds { get; set; } = 300;
}

public sealed class HealthCheckSettings
{
    public bool Enabled { get; set; }

    public HealthCheckType Type { get; set; } = HealthCheckType.HttpJson;

    public string Url { get; set; } = string.Empty;

    public int IntervalSeconds { get; set; } = 30;

    public int TimeoutSeconds { get; set; } = 5;

    public int FailureThreshold { get; set; } = 3;

    public int ExpectedStatusCode { get; set; } = 200;

    public string ExpectedJsonProperty { get; set; } = string.Empty;

    public string ExpectedJsonValue { get; set; } = string.Empty;
}

public sealed class LogSettings
{
    public string Directory { get; set; } = string.Empty;

    public int MaximumFileSizeMegabytes { get; set; } = 20;

    public int RetainedFileCount { get; set; } = 10;
}

public enum ServiceStartMode
{
    Automatic,
    AutomaticDelayedStart,
    Manual,
    Disabled
}

public enum RestartPolicy
{
    Never,
    OnFailure,
    Always
}

public enum HealthCheckType
{
    Process,
    HttpStatus,
    HttpJson
}