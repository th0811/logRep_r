namespace FfxiTempLogCollector.Core;

public sealed class TempLogMetaFields
{
    public IReadOnlyList<string> Fields { get; init; } = [];

    public string? EventGroup { get; init; }

    public string? SequenceHint { get; init; }

    public string? TemplateHint { get; init; }
}
