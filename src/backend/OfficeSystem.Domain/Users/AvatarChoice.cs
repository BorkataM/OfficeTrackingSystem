using System.Text.RegularExpressions;

namespace OfficeSystem.Domain.Users;

/// <summary>
/// A generated avatar, stored as "style:seed". The browser draws it from those two
/// values, so the server only has to keep the pair well-formed.
/// </summary>
public static partial class AvatarChoice
{
    public const int MaxLength = 64;

    public static readonly IReadOnlySet<string> Styles = new HashSet<string>(StringComparer.Ordinal)
    {
        "notionists",
        "lorelei",
        "open-peeps",
        "thumbs",
        "shapes",
        "glass",
    };

    public static bool IsValid(string value)
    {
        Match match = Format().Match(value);

        return match.Success && Styles.Contains(match.Groups["style"].Value);
    }

    [GeneratedRegex("^(?<style>[a-z-]{1,24}):[A-Za-z0-9]{1,32}$")]
    private static partial Regex Format();
}
