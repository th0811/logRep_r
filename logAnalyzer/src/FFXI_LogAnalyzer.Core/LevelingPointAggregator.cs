using System.Globalization;
using System.Text.RegularExpressions;

namespace FFXI_LogAnalyzer.Core;

public sealed partial class LevelingPointAggregator
{
    private static readonly string[] PointNames =
    [
        "経験値",
        "リミットポイント",
        "エクゼンプラーポイント"
    ];

    public IReadOnlyList<LevelingPointSummary> Aggregate(
        IEnumerable<CanonicalRecord> records,
        AnalysisTimeResult analysisTime)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(analysisTime);

        var totals = PointNames.ToDictionary(
            pointName => pointName,
            _ => 0L,
            StringComparer.Ordinal);

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.VisibleText))
            {
                continue;
            }

            var match = PointGainRegex().Match(record.VisibleText);
            if (!match.Success)
            {
                continue;
            }

            var pointName = match.Groups["pointName"].Value;
            if (!totals.ContainsKey(pointName))
            {
                continue;
            }

            totals[pointName] += long.Parse(
                match.Groups["points"].Value,
                CultureInfo.InvariantCulture);
        }

        return PointNames
            .Select(pointName => new LevelingPointSummary(
                pointName,
                totals[pointName],
                CalculatePointsPerHour(totals[pointName], analysisTime)))
            .ToArray();
    }

    private static double? CalculatePointsPerHour(
        long totalPoints,
        AnalysisTimeResult analysisTime)
    {
        if (analysisTime.Confidence == TimeConfidence.Unknown
            || !analysisTime.CanCalculateDps)
        {
            return null;
        }

        return totalPoints * 3600.0 / analysisTime.DurationSeconds!.Value;
    }

    [GeneratedRegex(@"^→?.+?は、(?<points>\d+)(?<pointName>経験値|リミットポイント|エクゼンプラーポイント)を獲得した。?$")]
    private static partial Regex PointGainRegex();
}
