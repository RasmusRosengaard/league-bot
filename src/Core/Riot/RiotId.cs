using System.Diagnostics.CodeAnalysis;

namespace LolMatchAlert.Core.Riot;

/// <summary>
/// Et Riot-ID på formen gameName#tagLine, f.eks. "Faker#KR1".
/// </summary>
public sealed record RiotId(string GameName, string TagLine)
{
    public override string ToString() => $"{GameName}#{TagLine}";

    /// <summary>
    /// Parser en streng på formen "gameName#tagLine". Tillader '#' i gameName ikke,
    /// men splitter på det sidste '#' så tagLine altid er sidste segment.
    /// </summary>
    public static bool TryParse(string? input, [NotNullWhen(true)] out RiotId? riotId)
    {
        riotId = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();
        var hashIndex = trimmed.LastIndexOf('#');
        if (hashIndex <= 0 || hashIndex == trimmed.Length - 1)
        {
            return false;
        }

        var gameName = trimmed[..hashIndex].Trim();
        var tagLine = trimmed[(hashIndex + 1)..].Trim();
        if (gameName.Length == 0 || tagLine.Length == 0)
        {
            return false;
        }

        riotId = new RiotId(gameName, tagLine);
        return true;
    }
}
