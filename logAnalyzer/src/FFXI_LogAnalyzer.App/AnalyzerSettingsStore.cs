using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FFXI_LogAnalyzer.Core;

namespace FFXI_LogAnalyzer.App;

public sealed class AnalyzerSettingsStore
{
    public const string FileName = "analyzer_settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AnalyzerSettingsStore(string? applicationDirectory = null)
    {
        var baseDirectory = Path.GetFullPath(
            applicationDirectory ?? AppContext.BaseDirectory);
        SettingsPath = Path.Combine(baseDirectory, FileName);
    }

    public string SettingsPath { get; }

    public AnalyzerSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new AnalyzerSettings();
        }

        try
        {
            using var stream = File.OpenRead(SettingsPath);
            return Normalize(
                JsonSerializer.Deserialize<AnalyzerSettings>(
                    stream,
                    JsonOptions)
                ?? new AnalyzerSettings());
        }
        catch (Exception)
        {
            return new AnalyzerSettings();
        }
    }

    public void Save(AnalyzerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var normalized = Normalize(settings);
        var directory = Path.GetDirectoryName(SettingsPath)
            ?? AppContext.BaseDirectory;
        Directory.CreateDirectory(directory);

        using var stream = new FileStream(
            SettingsPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);
        JsonSerializer.Serialize(stream, normalized, JsonOptions);
    }

    private static AnalyzerSettings Normalize(AnalyzerSettings settings)
    {
        return new AnalyzerSettings
        {
            SessionsRootFolderPath = NormalizeFolderPath(settings.SessionsRootFolderPath),
            KnownPcNames = NormalizePcNames(settings.KnownPcNames),
            KnownNpcNames = NormalizeNpcNames(settings.KnownNpcNames)
        };
    }

    private static string? NormalizeFolderPath(string? folderPath)
    {
        return string.IsNullOrWhiteSpace(folderPath)
            ? null
            : folderPath.Trim();
    }

    private static List<string> NormalizePcNames(IEnumerable<string> names)
    {
        return names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(ActorNameClassifier.NormalizePcName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> NormalizeNpcNames(IEnumerable<string> names)
    {
        return names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
