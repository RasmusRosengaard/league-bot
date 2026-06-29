using System.Diagnostics.CodeAnalysis;

namespace LolMatchAlert.Core.Riot;

/// <summary>
/// Oversætter brugervendte region-strenge (platform-id eller alias) til en
/// <see cref="Region"/> med korrekt regional routing-cluster.
/// </summary>
public static class Regions
{
    // Kanoniske platform-ids -> regional route. Aliaser tilføjes herunder.
    private static readonly Dictionary<string, Region> Map = BuildMap();

    private static Dictionary<string, Region> BuildMap()
    {
        var map = new Dictionary<string, Region>(StringComparer.OrdinalIgnoreCase);

        void Add(RegionalRoute route, string platform, params string[] aliases)
        {
            var region = new Region(platform, route);
            map[platform] = region;
            foreach (var alias in aliases)
            {
                map[alias] = region;
            }
        }

        // Europe
        Add(RegionalRoute.Europe, "euw1", "euw");
        Add(RegionalRoute.Europe, "eun1", "eune", "eun");
        Add(RegionalRoute.Europe, "tr1", "tr");
        Add(RegionalRoute.Europe, "ru");

        // Americas
        Add(RegionalRoute.Americas, "na1", "na");
        Add(RegionalRoute.Americas, "br1", "br");
        Add(RegionalRoute.Americas, "la1", "lan");
        Add(RegionalRoute.Americas, "la2", "las");

        // Asia
        Add(RegionalRoute.Asia, "kr");
        Add(RegionalRoute.Asia, "jp1", "jp");

        // Sea (match-v5 bruger 'sea' for disse platforme)
        Add(RegionalRoute.Sea, "oc1", "oce", "oc");
        Add(RegionalRoute.Sea, "ph2", "ph");
        Add(RegionalRoute.Sea, "sg2", "sg");
        Add(RegionalRoute.Sea, "th2", "th");
        Add(RegionalRoute.Sea, "tw2", "tw");
        Add(RegionalRoute.Sea, "vn2", "vn");

        return map;
    }

    /// <summary>Forsøg at parse en brugervendt region-streng.</summary>
    public static bool TryParse(string? input, [NotNullWhen(true)] out Region? region)
    {
        region = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return Map.TryGetValue(input.Trim(), out region);
    }

    /// <summary>Alle understøttede kanoniske platform-ids (til hjælpetekst/validering).</summary>
    public static IReadOnlyCollection<string> SupportedPlatforms { get; } =
        Map.Values.Select(r => r.Platform).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
}
