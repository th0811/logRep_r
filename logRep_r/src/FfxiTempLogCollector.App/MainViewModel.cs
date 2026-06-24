using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using FfxiTempLogCollector.Core;

namespace FfxiTempLogCollector.App;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly GuiCommandController _controller;
    private readonly Dispatcher _dispatcher;

    private string _statusText = "停止中";
    private string _statusForeground = "#FFFFFF";
    private string _statusBackground = "#162331";
    private string _sessionId = "-";
    private long _rawRecordsWritten;
    private long _canonicalRecordsWritten;
    private string _lastSeenAt = "-";
    private long _warningCount;
    private string _lastError = "-";
    private bool _isShuttingDown;

    public MainViewModel(
        GuiCommandController controller,
        Dispatcher dispatcher)
    {
        _controller = controller
            ?? throw new ArgumentNullException(nameof(controller));
        _dispatcher = dispatcher
            ?? throw new ArgumentNullException(nameof(dispatcher));

        StartCommand = new AsyncRelayCommand(
            StartAsync,
            () => !IsShuttingDown
                && (Status is CollectorStatus.Stopped
                    or CollectorStatus.Error));
        StopCommand = new AsyncRelayCommand(
            StopAsync,
            () => !IsShuttingDown
                && (Status is CollectorStatus.Starting
                    or CollectorStatus.Running));
        SettingsCommand = new RelayCommand(
            _controller.ShowSettings,
            () => !IsShuttingDown);
        OpenOutputCommand = new RelayCommand(
            _controller.OpenOutputDirectory,
            () => !IsShuttingDown);
        OpenLogCommand = new RelayCommand(
            _controller.OpenLog,
            () => !IsShuttingDown);
        MinimizeCommand = new RelayCommand(
            _controller.MinimizeToTray,
            () => !IsShuttingDown);
        ExitCommand = new RelayCommand(
            _controller.Exit,
            () => !IsShuttingDown);

        _controller.Events.StatusChanged += OnStatusChanged;
        _controller.ConfigChanged += OnConfigChanged;
        ApplyStatus(_controller.GetStatus());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CollectorStatus Status { get; private set; }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string StatusForeground
    {
        get => _statusForeground;
        private set => SetProperty(ref _statusForeground, value);
    }

    public string StatusBackground
    {
        get => _statusBackground;
        private set => SetProperty(ref _statusBackground, value);
    }

    public bool IsShuttingDown
    {
        get => _isShuttingDown;
        private set => SetProperty(ref _isShuttingDown, value);
    }

    public string TempDirectory => _controller.Config.TempDir;

    public string OutputDirectory => _controller.Config.OutputDir;

    public string SessionId
    {
        get => _sessionId;
        private set => SetProperty(ref _sessionId, value);
    }

    public long RawRecordsWritten
    {
        get => _rawRecordsWritten;
        private set => SetProperty(ref _rawRecordsWritten, value);
    }

    public long CanonicalRecordsWritten
    {
        get => _canonicalRecordsWritten;
        private set => SetProperty(
            ref _canonicalRecordsWritten,
            value);
    }

    public string LastSeenAt
    {
        get => _lastSeenAt;
        private set => SetProperty(ref _lastSeenAt, value);
    }

    public long WarningCount
    {
        get => _warningCount;
        private set => SetProperty(ref _warningCount, value);
    }

    public string LastError
    {
        get => _lastError;
        private set => SetProperty(ref _lastError, value);
    }

    public AsyncRelayCommand StartCommand { get; }

    public AsyncRelayCommand StopCommand { get; }

    public RelayCommand SettingsCommand { get; }

    public RelayCommand OpenOutputCommand { get; }

    public RelayCommand OpenLogCommand { get; }

    public RelayCommand MinimizeCommand { get; }

    public RelayCommand ExitCommand { get; }

    public async Task InitializeAsync()
    {
        if (_controller.Config.AutoStartCollectionOnLaunch
            && (Status is CollectorStatus.Stopped
                or CollectorStatus.Error))
        {
            await StartAsync();
        }
    }

    public async Task ShutdownAsync()
    {
        _controller.Events.StatusChanged -= OnStatusChanged;
        _controller.ConfigChanged -= OnConfigChanged;
        await _controller.DisposeAsync();
    }

    public WindowCloseAction GetCloseAction()
    {
        return _controller.GetCloseAction();
    }

    public void BeginShutdown()
    {
        if (IsShuttingDown)
        {
            return;
        }

        IsShuttingDown = true;
        StatusText = "終了しています...";
        StatusForeground = "#FFFFFF";
        StatusBackground = "#B45309";
        RaiseCommandCanExecuteChanged();
    }

    public void MinimizeToTray()
    {
        _controller.MinimizeToTray();
    }

    public void HandleWindowStateChanged()
    {
        _controller.HandleWindowStateChanged();
    }

    private async Task StartAsync()
    {
        try
        {
            var started = await _controller.StartAsync();

            if (started
                && _controller.Config.MinimizeToTrayWhileCollecting)
            {
                _controller.MinimizeToTray();
            }
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
        }
    }

    private async Task StopAsync()
    {
        try
        {
            await _controller.StopAsync();
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
        }
    }

    private void OnStatusChanged(
        object? sender,
        CollectorStatusSnapshot snapshot)
    {
        _dispatcher.InvokeAsync(() => ApplyStatus(snapshot));
    }

    private void OnConfigChanged(object? sender, EventArgs eventArgs)
    {
        OnPropertyChanged(nameof(TempDirectory));
        OnPropertyChanged(nameof(OutputDirectory));
    }

    private void ApplyStatus(CollectorStatusSnapshot snapshot)
    {
        if (IsShuttingDown)
        {
            return;
        }

        Status = snapshot.Status;
        StatusText = GetStatusText(snapshot.Status);
        StatusForeground = GetStatusForeground(snapshot.Status);
        StatusBackground = GetStatusBackground(snapshot.Status);
        SessionId = string.IsNullOrWhiteSpace(snapshot.SessionId)
            ? "-"
            : snapshot.SessionId;
        RawRecordsWritten = snapshot.RawRecordsWritten;
        CanonicalRecordsWritten =
            snapshot.CanonicalRecordsWritten;
        LastSeenAt = snapshot.LastSeenAt?.ToLocalTime()
            .ToString("yyyy/MM/dd HH:mm:ss")
            ?? "-";
        WarningCount = snapshot.WarningCount;
        LastError = string.IsNullOrWhiteSpace(snapshot.LastError)
            ? "-"
            : snapshot.LastError;

        RaiseCommandCanExecuteChanged();
    }

    private static string GetStatusText(CollectorStatus status)
    {
        return status switch
        {
            CollectorStatus.Stopped => "停止中",
            CollectorStatus.Starting => "開始処理中",
            CollectorStatus.Running => "収集中",
            CollectorStatus.Stopping => "停止処理中",
            CollectorStatus.Error => "エラー",
            _ => "不明",
        };
    }

    private static string GetStatusForeground(CollectorStatus status)
    {
        return status switch
        {
            CollectorStatus.Running => "#ECFDF5",
            CollectorStatus.Starting => "#FFFBEB",
            CollectorStatus.Stopping => "#FFFBEB",
            CollectorStatus.Error => "#FEF2F2",
            _ => "#FFFFFF",
        };
    }

    private static string GetStatusBackground(CollectorStatus status)
    {
        return status switch
        {
            CollectorStatus.Running => "#047857",
            CollectorStatus.Starting => "#B45309",
            CollectorStatus.Stopping => "#B45309",
            CollectorStatus.Error => "#B42318",
            _ => "#162331",
        };
    }

    private void RaiseCommandCanExecuteChanged()
    {
        StartCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
        SettingsCommand.RaiseCanExecuteChanged();
        OpenOutputCommand.RaiseCanExecuteChanged();
        OpenLogCommand.RaiseCanExecuteChanged();
        MinimizeCommand.RaiseCanExecuteChanged();
        ExitCommand.RaiseCanExecuteChanged();
    }

    private void SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
