using System.Text.RegularExpressions;

namespace FFXI_LogAnalyzer.Core;

public sealed partial class ActionNameExtractor : IActionNameExtractor
{
    private readonly NormalAttackParser _normalAttackParser;

    public ActionNameExtractor()
        : this(new NormalAttackParser())
    {
    }

    public ActionNameExtractor(NormalAttackParser normalAttackParser)
    {
        _normalAttackParser = normalAttackParser;
    }

    public string? ExtractActionName(ActionGroup group)
    {
        if (_normalAttackParser.TryParse(group, out var normalAttack))
        {
            return normalAttack.ActionName;
        }

        foreach (var text in group.VisibleTexts)
        {
            var actionName = ExtractActionName(text);
            if (!string.IsNullOrWhiteSpace(actionName))
            {
                return actionName;
            }
        }

        return null;
    }

    public ActionType ExtractActionType(ActionGroup group)
    {
        if (_normalAttackParser.TryParse(group, out var normalAttack))
        {
            return normalAttack.ActionType;
        }

        foreach (var text in group.VisibleTexts)
        {
            if (SkillRegex().IsMatch(text))
            {
                return ActionType.Skill;
            }

            if (MagicRegex().IsMatch(text))
            {
                return ActionType.Magic;
            }
        }

        return ActionType.Unknown;
    }

    private static string? ExtractActionName(string text)
    {
        if (NormalAttackRegex().IsMatch(text))
        {
            return "通常攻撃";
        }

        var skill = SkillRegex().Match(text);
        if (skill.Success)
        {
            return skill.Groups["action"].Value;
        }

        var magic = MagicRegex().Match(text);
        if (magic.Success)
        {
            return magic.Groups["action"].Value;
        }

        var activated = ActivatedRegex().Match(text);
        return activated.Success ? activated.Groups["action"].Value : null;
    }

    [GeneratedRegex(@"^.+?の攻撃")]
    private static partial Regex NormalAttackRegex();

    [GeneratedRegex(@"^.+?は、(?<action>.+?)を実行")]
    private static partial Regex SkillRegex();

    [GeneratedRegex(@"^.+?は、(?<action>.+?)を唱えた")]
    private static partial Regex MagicRegex();

    [GeneratedRegex(@"^.+?の(?<action>.+?)が発動")]
    private static partial Regex ActivatedRegex();
}
