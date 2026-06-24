namespace FFXI_LogAnalyzer.Core;

public sealed record MarkerRecord(
    long? Order,
    string? MarkerKeyword,
    string? VisibleText,
    string? MessageTimeText,
    DateTimeOffset? FirstSeenAt,
    CanonicalRecord SourceRecord);
