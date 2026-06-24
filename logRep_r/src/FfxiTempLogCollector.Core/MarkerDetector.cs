using System.Text.RegularExpressions;

namespace FfxiTempLogCollector.Core;

public sealed partial class MarkerDetector
{
    public DetectedMarker? Detect(string visibleText)
    {
        ArgumentNullException.ThrowIfNull(visibleText);

        var match = MarkerRegex().Match(visibleText);

        if (!match.Success)
        {
            return null;
        }

        return new DetectedMarker
        {
            Keyword = match.Groups["keyword"].Value,
        };
    }

    [GeneratedRegex(
        @"#(?<keyword>[A-Za-z0-9_\-:.]+|[^\s　]+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex MarkerRegex();
}
