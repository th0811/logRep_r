using FFXI_LogAnalyzer.Core;

namespace FFXI_LogAnalyzer.App;

public sealed class ActorSummaryViewModel
{
    public ActorSummaryViewModel(ActorSummary summary)
    {
        Actor = summary.Actor;
        TotalDamage = summary.TotalDamage.ToString("N0");
        Dps = FormatNullable(summary.Dps);
        DpsTimeConfidence = summary.DpsTimeConfidence.ToString();
        NormalAttackHitRate = FormatRate(summary.NormalAttackHitRate);
        NormalAttackCriticalRate = FormatRate(summary.NormalAttackCriticalRate);
        TotalUseCount = summary.TotalUseCount.ToString("N0");
        TotalHitCount = summary.TotalHitCount.ToString("N0");
        TotalMissCount = summary.TotalMissCount.ToString("N0");
        UnknownCount = summary.UnknownCount.ToString("N0");
    }

    public string Actor { get; }

    public string TotalDamage { get; }

    public string Dps { get; }

    public string DpsTimeConfidence { get; }

    public string NormalAttackHitRate { get; }

    public string NormalAttackCriticalRate { get; }

    public string TotalUseCount { get; }

    public string TotalHitCount { get; }

    public string TotalMissCount { get; }

    public string UnknownCount { get; }

    private static string FormatRate(double? rate)
    {
        return rate is null ? "-" : $"{rate:P1}";
    }

    private static string FormatNullable(double? value)
    {
        return value is null ? "-" : value.Value.ToString("0.###");
    }
}
