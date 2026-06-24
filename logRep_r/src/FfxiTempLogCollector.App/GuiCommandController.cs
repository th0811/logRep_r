using System.Diagnostics;
using System.IO;
using System.Windows;
using FfxiTempLogCollector.Core;
using FfxiTempLogCollector.Ipc;
using MessageBox = System.Windows.MessageBox;

namespace FfxiTempLogCollector.App;

public sealed class GuiCommandController : IAsyncDisposable
{
    private const string CollectorLogFileName = "collector.log";

    private readonly CollectorService _collectorService;
    private readonly ConfigEditService _configEditService;
    private readonly FolderPickerService _folderPickerService;
    private readonly ConfigLoader _configLoader;
    private Window? _window;
    private TrayMenuController? _trayMenuController;
    private TrayIconController? _trayIconController;
    private WindowCloseBehaviorController? _closeBehaviorController;
    private bool _disposed;
    private bool _exitRequested;

    public GuiCommandController(
        CollectorService collectorService,
        CollectorConfig config,
        ConfigEditService configEditService,
        ConfigLoader configLoader,
        FolderPickerService? folderPickerService = null)
    {
        _collectorService = collectorService
            ?? throw new ArgumentNullException(nameof(collectorService));
        Config = config
            ?? throw new ArgumentNullException(nameof(config));
        _configEditService = configEditService
            ?? throw new ArgumentNullException(
                nameof(configEditService));
        _configLoader = configLoader
            ?? throw new ArgumentNullException(nameof(configLoader));
        _folderPickerService = folderPickerService
            ?? new FolderPickerService();
    }

    public CollectorConfig Config { get; }

    public CollectorEvents Events => _collectorService.Events;

    public event EventHandler? ConfigChanged;

    public void AttachWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (_window is not null)
        {
            throw new InvalidOperationException(
                "メインウィンドウは既に設定されています。");
        }

        _window = window;
        _trayMenuController = new TrayMenuController(this);
        _trayIconController = new TrayIconController(
            window,
            _collectorService,
            () => Config,
            _trayMenuController);
        _closeBehaviorController =
            new WindowCloseBehaviorController(
                window,
                () => Config,
                GetStatus);
    }

    public CollectorStatusSnapshot GetStatus()
    {
        return _collectorService.GetStatus();
    }

    public async Task<IpcResponse> HandleIpcCommandAsync(
        IpcCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command.Name.ToLowerInvariant() switch
        {
            "status" => CreateStatusResponse(),
            "start" => await HandleStartIpcAsync(cancellationToken),
            "stop" => await HandleStopIpcAsync(cancellationToken),
            "show" => HandleShowIpc(),
            "config-updated" => HandleConfigUpdatedIpc(command),
            _ => IpcResponse.Error(
                $"未対応のIPCコマンドです: {command.Name}"),
        };
    }

    public Task<bool> StartAsync(
        CancellationToken cancellationToken = default)
    {
        return _collectorService.StartAsync(
            new CollectorStartRequest
            {
                Config = ConfigEditService.Clone(Config),
            },
            cancellationToken);
    }

    public Task StopAsync(
        CancellationToken cancellationToken = default)
    {
        return _collectorService.StopAsync(
            new CollectorStopRequest(),
            cancellationToken);
    }

    public void ShowSettings()
    {
        var settingsWindow = new SettingsWindow
        {
            Owner = GetWindow(),
        };
        var viewModel = new SettingsViewModel(
            settingsWindow,
            Config,
            _configEditService,
            _folderPickerService);
        viewModel.ConfigSaved += (_, _) =>
            ConfigChanged?.Invoke(this, EventArgs.Empty);
        settingsWindow.DataContext = viewModel;
        settingsWindow.ShowDialog();
    }

    public void OpenOutputDirectory()
    {
        OpenDirectory(Config.OutputDir, "出力先フォルダー");
    }

    public void OpenLog()
    {
        var status = GetStatus();
        var directory = status.SessionDirectory ?? Config.OutputDir;
        var logPath = Path.Combine(directory, CollectorLogFileName);

        if (File.Exists(logPath))
        {
            StartShell(logPath);
            return;
        }

        OpenDirectory(directory, "ログフォルダー");
    }

    public void MinimizeToTray()
    {
        GetTrayIconController().MoveWindowToTray();
    }

    public void ShowWindow()
    {
        GetTrayIconController().ShowWindow();
    }

    public void HandleWindowStateChanged()
    {
        if (GetWindow().WindowState == WindowState.Minimized
            && GetCloseBehaviorController()
                .ShouldMoveMinimizedWindowToTray())
        {
            MinimizeToTray();
        }
    }

    public void Exit()
    {
        _exitRequested = true;
        GetWindow().Close();
    }

    public WindowCloseAction GetCloseAction()
    {
        return GetCloseBehaviorController()
            .GetCloseAction(_exitRequested);
    }

    public void ShowOperationError(
        string message,
        Exception exception)
    {
        var detail = $"{message}\n{exception.Message}";
        MessageBox.Show(
            GetWindow(),
            detail,
            "操作エラー",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        _trayIconController?.NotifyOutputFailure(detail);
    }

    private IpcResponse CreateStatusResponse()
    {
        var status = GetStatus();
        return IpcResponse.Ok(
            data: new Dictionary<string, string>
            {
                ["status"] =
                    status.Status.ToString().ToLowerInvariant(),
                ["session_id"] = status.SessionId ?? string.Empty,
                ["raw_records_written"] =
                    status.RawRecordsWritten.ToString(),
                ["canonical_records_written"] =
                    status.CanonicalRecordsWritten.ToString(),
                ["last_error"] = status.LastError ?? string.Empty,
            });
    }

    private async Task<IpcResponse> HandleStartIpcAsync(
        CancellationToken cancellationToken)
    {
        var started = await StartAsync(cancellationToken);
        return started
            ? IpcResponse.Ok("ログ収集を開始しました。")
            : IpcResponse.Error(
                GetStatus().LastError
                ?? "ログ収集は既に開始されています。");
    }

    private async Task<IpcResponse> HandleStopIpcAsync(
        CancellationToken cancellationToken)
    {
        await StopAsync(cancellationToken);
        return IpcResponse.Ok("ログ収集を停止しました。");
    }

    private IpcResponse HandleShowIpc()
    {
        GetWindow().Dispatcher.BeginInvoke(ShowWindow);
        return IpcResponse.Ok("メイン画面を表示しました。");
    }

    private IpcResponse HandleConfigUpdatedIpc(IpcCommand command)
    {
        command.Arguments.TryGetValue(
            "config_path",
            out var configPath);
        var updated = _configLoader.Load(configPath);
        ConfigEditService.CopyConfig(updated, Config);
        _collectorService.UpdatePollingInterval(
            Config.PollingIntervalMs);
        _collectorService.UpdateLogLevel(Config.LogLevel);
        GetWindow().Dispatcher.BeginInvoke(
            () => ConfigChanged?.Invoke(this, EventArgs.Empty));
        return IpcResponse.Ok("設定変更を反映しました。");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _trayIconController?.Dispose();
        _trayMenuController?.Dispose();
        await _collectorService.DisposeAsync();
    }

    private void OpenDirectory(string path, string displayName)
    {
        try
        {
            Directory.CreateDirectory(path);
            StartShell(path);
        }
        catch (Exception exception)
        {
            ShowOperationError(
                $"{displayName}を開けませんでした。",
                exception);
        }
    }

    private static void StartShell(string path)
    {
        Process.Start(
            new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
    }

    private Window GetWindow()
    {
        return _window
            ?? throw new InvalidOperationException(
                "メインウィンドウが設定されていません。");
    }

    private TrayIconController GetTrayIconController()
    {
        return _trayIconController
            ?? throw new InvalidOperationException(
                "タスクトレイアイコンが初期化されていません。");
    }

    private WindowCloseBehaviorController GetCloseBehaviorController()
    {
        return _closeBehaviorController
            ?? throw new InvalidOperationException(
                "ウィンドウ動作が初期化されていません。");
    }
}
