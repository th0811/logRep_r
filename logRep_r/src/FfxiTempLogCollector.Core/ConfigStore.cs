namespace FfxiTempLogCollector.Core;

public sealed class ConfigStore
{
    public const string ConfigFileName = "config.json";

    public const string ApplicationDirectoryName = "FFXI_LogRep_r";

    public const string LegacyApplicationDirectoryName =
        "FfxiTempLogCollector";

    public ConfigStore(string? appDataDirectory = null)
    {
        var baseDirectory = appDataDirectory
            ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        DefaultPath = Path.Combine(
            baseDirectory,
            ApplicationDirectoryName,
            ConfigFileName);
        LegacyDefaultPath = Path.Combine(
            baseDirectory,
            LegacyApplicationDirectoryName,
            ConfigFileName);
    }

    public string DefaultPath { get; }

    public string LegacyDefaultPath { get; }

    public CollectorConfig Load(string? path = null)
    {
        return JsonFileSerializer.Load<CollectorConfig>(path ?? DefaultPath);
    }

    public void Save(CollectorConfig config, string? path = null)
    {
        JsonFileSerializer.Save(path ?? DefaultPath, config);
    }
}
