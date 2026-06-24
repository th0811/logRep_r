using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FFXI_LogAnalyzer.Core;

namespace FFXI_LogAnalyzer.App;

public sealed class AnalysisResultViewModel : INotifyPropertyChanged
{
    private readonly ActorFilterService _actorFilterService = new();
    private readonly List<ActorSummaryViewModel> _allActorSummaries = [];
    private readonly List<ActionSummaryViewModel> _allActionSummaries = [];
    private bool _hasResult;
    private string _statusMessage = "分析結果はまだありません。";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ActorSummaryViewModel> ActorSummaries { get; } = [];

    public ObservableCollection<ActionSummaryViewModel> ActionSummaries { get; } = [];

    public ObservableCollection<ActorVisibilityViewModel> ActorVisibilities { get; } = [];

    public ObservableCollection<UnparsedLogViewModel> UnparsedLogs { get; } = [];

    public ObservableCollection<SessionInfoRow> SessionRows { get; } = [];

    public bool HasResult
    {
        get => _hasResult;
        private set => SetProperty(ref _hasResult, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public void Load(AnalysisResult result, IReadOnlyList<SessionInfoRow> sessionRows)
    {
        ClearVisibilitySubscriptions();

        _allActorSummaries.Clear();
        _allActorSummaries.AddRange(result.ActorSummaries.Select(summary => new ActorSummaryViewModel(summary)));

        _allActionSummaries.Clear();
        _allActionSummaries.AddRange(result.ActionSummaries.Select(summary => new ActionSummaryViewModel(summary)));

        ActorVisibilities.Clear();
        foreach (var actor in _allActorSummaries.Select(summary => summary.Actor).Distinct(StringComparer.Ordinal))
        {
            var visibility = new ActorVisibilityViewModel(actor);
            visibility.PropertyChanged += OnActorVisibilityChanged;
            ActorVisibilities.Add(visibility);
        }

        UnparsedLogs.Clear();
        foreach (var unknownActionGroup in result.UnknownActionGroups)
        {
            UnparsedLogs.Add(new UnparsedLogViewModel(unknownActionGroup));
        }

        foreach (var unparsedActionGroup in result.UnparsedActionGroups)
        {
            UnparsedLogs.Add(new UnparsedLogViewModel(unparsedActionGroup));
        }

        SessionRows.Clear();
        foreach (var row in sessionRows)
        {
            SessionRows.Add(row);
        }

        SessionRows.Add(new SessionInfoRow("time_confidence", ToConfidenceText(result.AnalysisTime.Confidence)));
        SessionRows.Add(new SessionInfoRow("duration_seconds", result.AnalysisTime.DurationSeconds?.ToString("0.###") ?? "-"));
        SessionRows.Add(new SessionInfoRow("DPS状態", ToDpsStatus(result.AnalysisTime)));
        SessionRows.Add(new SessionInfoRow("未解析ログ件数", UnparsedLogs.Count.ToString("N0")));

        RefreshFilteredResults();

        HasResult = true;
        StatusMessage = "分析結果を表示しています。";
    }

    public void Clear()
    {
        ClearVisibilitySubscriptions();
        _allActorSummaries.Clear();
        _allActionSummaries.Clear();
        ActorSummaries.Clear();
        ActionSummaries.Clear();
        ActorVisibilities.Clear();
        UnparsedLogs.Clear();
        SessionRows.Clear();
        HasResult = false;
        StatusMessage = "分析結果はまだありません。";
    }

    private static string ToConfidenceText(TimeConfidence confidence)
    {
        return confidence switch
        {
            TimeConfidence.Minute => "Minute（分精度による概算）",
            TimeConfidence.Estimated => "Estimated（推定時刻による概算）",
            TimeConfidence.Unknown => "Unknown（分析時間を確定できないためDPS計算不可）",
            _ => confidence.ToString()
        };
    }

    private static string ToDpsStatus(AnalysisTimeResult analysisTime)
    {
        return analysisTime.CanCalculateDps && analysisTime.Confidence != TimeConfidence.Unknown
            ? "DPS計算可能"
            : "DPS計算不可";
    }

    private void OnActorVisibilityChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ActorVisibilityViewModel.IsVisible))
        {
            RefreshFilteredResults();
        }
    }

    private void RefreshFilteredResults()
    {
        ActorSummaries.Clear();
        foreach (var summary in _actorFilterService.FilterActorSummaries(_allActorSummaries, ActorVisibilities))
        {
            ActorSummaries.Add(summary);
        }

        ActionSummaries.Clear();
        foreach (var summary in _actorFilterService.FilterActionSummaries(_allActionSummaries, ActorVisibilities))
        {
            ActionSummaries.Add(summary);
        }
    }

    private void ClearVisibilitySubscriptions()
    {
        foreach (var visibility in ActorVisibilities)
        {
            visibility.PropertyChanged -= OnActorVisibilityChanged;
        }
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
