using FfxiTempLogCollector.Core;

namespace FfxiTempLogCollector.Tests;

public sealed class ConfigLoaderTests
{
    [Fact]
    public void 環境変数を含むパスを展開できる()
    {
        var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");

        Assert.False(string.IsNullOrWhiteSpace(userProfile));

        var actual = ConfigLoader.ExpandPath(
            @"%USERPROFILE%\Documents\FFXI_LogRep_r\sessions");

        Assert.Equal(
            Path.Combine(
                userProfile!,
                "Documents",
                "FFXI_LogRep_r",
                "sessions"),
            actual);
    }

    [Fact]
    public void 明示パスを最優先で読み込める()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var applicationDirectory = temporaryDirectory.GetPath("application");
        var appDataDirectory = temporaryDirectory.GetPath("appdata");
        var explicitPath = temporaryDirectory.GetPath("explicit.json");
        Directory.CreateDirectory(applicationDirectory);

        var store = new ConfigStore(appDataDirectory);
        store.Save(
            new CollectorConfig { LogLevel = "debug" },
            Path.Combine(applicationDirectory, ConfigStore.ConfigFileName));
        store.Save(new CollectorConfig { LogLevel = "warning" });
        store.Save(new CollectorConfig { LogLevel = "error" }, explicitPath);

        var loader = new ConfigLoader(store, applicationDirectory);
        var actual = loader.Load(explicitPath);

        Assert.Equal("error", actual.LogLevel);
    }

    [Fact]
    public void 実行ディレクトリをAppDataより優先して読み込める()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var applicationDirectory = temporaryDirectory.GetPath("application");
        var appDataDirectory = temporaryDirectory.GetPath("appdata");
        Directory.CreateDirectory(applicationDirectory);

        var store = new ConfigStore(appDataDirectory);
        store.Save(
            new CollectorConfig { LogLevel = "debug" },
            Path.Combine(applicationDirectory, ConfigStore.ConfigFileName));
        store.Save(new CollectorConfig { LogLevel = "warning" });

        var loader = new ConfigLoader(store, applicationDirectory);
        var actual = loader.Load();

        Assert.Equal("debug", actual.LogLevel);
    }

    [Fact]
    public void 実行ディレクトリになければAppDataから読み込める()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var applicationDirectory = temporaryDirectory.GetPath("application");
        var store = new ConfigStore(temporaryDirectory.GetPath("appdata"));
        store.Save(new CollectorConfig { LogLevel = "warning" });

        var loader = new ConfigLoader(store, applicationDirectory);
        var actual = loader.Load();

        Assert.Equal("warning", actual.LogLevel);
    }

    [Fact]
    public void 新設定がなければ旧AppData設定を読み込める()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var applicationDirectory = temporaryDirectory.GetPath("application");
        var store = new ConfigStore(temporaryDirectory.GetPath("appdata"));
        store.Save(
            new CollectorConfig { LogLevel = "warning" },
            store.LegacyDefaultPath);

        var loader = new ConfigLoader(store, applicationDirectory);

        Assert.Equal(store.LegacyDefaultPath, loader.ResolvePath());
        Assert.Equal("warning", loader.Load().LogLevel);
    }

    [Fact]
    public void 設定ファイルがなければ展開済みデフォルト設定を返す()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = new ConfigStore(temporaryDirectory.GetPath("appdata"));
        var loader = new ConfigLoader(
            store,
            temporaryDirectory.GetPath("application"));

        var actual = loader.Load();

        Assert.DoesNotContain("%USERPROFILE%", actual.OutputDir);
        Assert.Equal("cp932", actual.Encoding);
    }
}
