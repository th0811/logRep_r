using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FFXI_LogAnalyzer.Core;

namespace FFXI_LogAnalyzer.App;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly SessionOpenService _sessionOpenService;
    private readonly DialogService _dialogService;
    private readonly CanonicalRecordReader _canonicalRecordReader = new();
    private AnalyzerInputSession? _currentSession;
    private string _statusMessage = "セッションフォルダを選択してください。";
    private string _selectedFolderPath = "未選択";

    public MainViewModel(SessionOpenService sessionOpenService, DialogService dialogService)
    {
        _sessionOpenService = sessionOpenService;
        _dialogService = dialogService;
        AnalysisRange.AnalysisCompleted += OnAnalysisCompleted;
        OpenSessionCommand = new RelayCommand(OpenSession);
        ProceedToRangeSelectionCommand = new RelayCommand(ProceedToRangeSelection, () => HasSession);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public RelayCommand OpenSessionCommand { get; }

    public RelayCommand ProceedToRangeSelectionCommand { get; }

    public ObservableCollection<SessionInfoRow> SessionInfoRows { get; } = [];

    public ObservableCollection<string> Warnings { get; } = [];

    public AnalysisRangeViewModel AnalysisRange { get; } = new();

    public AnalysisResultViewModel AnalysisResult { get; } = new();

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string SelectedFolderPath
    {
        get => _selectedFolderPath;
        private set => SetProperty(ref _selectedFolderPath, value);
    }

    public bool HasSession => _currentSession is not null;

    private void OpenSession()
    {
        var folderPath = _dialogService.SelectSessionFolder();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            StatusMessage = "セッションフォルダ選択をキャンセルしました。";
            return;
        }

        StatusMessage = "セッションを読み込んでいます。";
        SelectedFolderPath = folderPath;

        var result = _sessionOpenService.Open(folderPath);
        if (!result.IsSuccess || result.Session is null)
        {
            ClearSession();
            var message = string.Join(Environment.NewLine, result.Errors);
            StatusMessage = "セッションの読み込みに失敗しました。";
            _dialogService.ShowError(message);
            return;
        }

        if (result.Warnings.Count > 0 && !_dialogService.ConfirmWarnings(result.Warnings))
        {
            ClearSession();
            StatusMessage = "警告があるセッションの読み込みをキャンセルしました。";
            return;
        }

        var canonicalRecords = _canonicalRecordReader.Read(result.Session.CanonicalRecordsPath);
        if (!canonicalRecords.IsSuccess)
        {
            ClearSession();
            var message = string.Join(Environment.NewLine, canonicalRecords.Errors);
            StatusMessage = "canonical_records.jsonl の読み込みに失敗しました。";
            _dialogService.ShowError(message);
            return;
        }

        var warnings = result.Warnings
            .Concat(canonicalRecords.LineErrors.Select(error => $"canonical_records.jsonl {error.LineNumber}行目: {error.Message}"))
            .ToArray();

        SetSession(result.Session, canonicalRecords.Records, warnings);
        StatusMessage = "セッションを読み込みました。";
    }

    private void ProceedToRangeSelection()
    {
        _dialogService.ShowInformation("分析区間タブで開始・終了ポイントを選択してください。");
    }

    private void SetSession(
        AnalyzerInputSession session,
        IReadOnlyList<CanonicalRecord> canonicalRecords,
        IReadOnlyList<string> warnings)
    {
        _currentSession = session;
        SessionInfoRows.Clear();
        foreach (var row in BuildSessionRows(session))
        {
            SessionInfoRows.Add(row);
        }

        Warnings.Clear();
        foreach (var warning in warnings)
        {
            Warnings.Add(warning);
        }

        AnalysisRange.LoadRecords(canonicalRecords);
        AnalysisResult.Clear();
        OnPropertyChanged(nameof(HasSession));
        ProceedToRangeSelectionCommand.RaiseCanExecuteChanged();
    }

    private void ClearSession()
    {
        _currentSession = null;
        SessionInfoRows.Clear();
        Warnings.Clear();
        AnalysisRange.Clear();
        AnalysisResult.Clear();
        OnPropertyChanged(nameof(HasSession));
        ProceedToRangeSelectionCommand.RaiseCanExecuteChanged();
    }

    private void OnAnalysisCompleted(AnalysisResult result)
    {
        AnalysisResult.Load(result, SessionInfoRows.ToArray());
        StatusMessage = "分析結果を表示しました。";
    }

    private static IReadOnlyList<SessionInfoRow> BuildSessionRows(AnalyzerInputSession session)
    {
        var info = session.SessionInfo;
        var stats = session.StatsInfo;
        return
        [
            new SessionInfoRow("session_id", ToDisplay(info.SessionId)),
            new SessionInfoRow("status", info.Status.ToString()),
            new SessionInfoRow("started_at", ToDisplay(info.StartedAt)),
            new SessionInfoRow("ended_at", ToDisplay(info.EndedAt)),
            new SessionInfoRow("collector_version", ToDisplay(info.CollectorVersion)),
            new SessionInfoRow("schema_version", ToDisplay(info.SchemaVersion)),
            new SessionInfoRow("raw_schema_version", ToDisplay(info.RawSchemaVersion)),
            new SessionInfoRow("canonical_schema_version", ToDisplay(info.CanonicalSchemaVersion)),
            new SessionInfoRow("canonical record件数", stats.CanonicalRecordsWritten.ToString("N0")),
            new SessionInfoRow("gap_warnings", stats.GapWarnings.ToString("N0")),
            new SessionInfoRow("parse_errors", stats.ParseErrors.ToString("N0")),
            new SessionInfoRow("decode_errors", stats.DecodeErrors.ToString("N0"))
        ];
    }

    private static string ToDisplay(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string ToDisplay(DateTimeOffset? value)
    {
        return value?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "-";
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
