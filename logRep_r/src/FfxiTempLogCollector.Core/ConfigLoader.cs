namespace FfxiTempLogCollector.Core;

public sealed class ConfigLoader
{
    private readonly string _applicationDirectory;
    private readonly ConfigStore _configStore;

    public ConfigLoader(
        ConfigStore? configStore = null,
        string? applicationDirectory = null)
    {
        _configStore = configStore ?? new ConfigStore();
        _applicationDirectory = Path.GetFullPath(
            applicationDirectory ?? AppContext.BaseDirectory);
    }

    public CollectorConfig Load(string? explicitPath = null)
    {
        var configPath = ResolvePath(explicitPath);

        if (configPath is null)
        {
            return ExpandPaths(new CollectorConfig());
        }

        return ExpandPaths(_configStore.Load(configPath));
    }

    public string? ResolvePath(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return Path.GetFullPath(ExpandPath(explicitPath));
        }

        var applicationConfigPath = Path.Combine(
            _applicationDirectory,
            ConfigStore.ConfigFileName);

        if (File.Exists(applicationConfigPath))
        {
            return applicationConfigPath;
        }

        if (File.Exists(_configStore.DefaultPath))
        {
            return _configStore.DefaultPath;
        }

        return File.Exists(_configStore.LegacyDefaultPath)
            ? _configStore.LegacyDefaultPath
            : null;
    }

    public static string ExpandPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return Environment.ExpandEnvironmentVariables(path);
    }

    private static CollectorConfig ExpandPaths(CollectorConfig config)
    {
        config.TempDir = ExpandPath(config.TempDir);
        config.OutputDir = ExpandPath(config.OutputDir);

        return config;
    }
}
