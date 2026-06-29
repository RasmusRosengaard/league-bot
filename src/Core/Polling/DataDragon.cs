namespace LolMatchAlert.Core.Polling;

/// <summary>
/// Bygger Data Dragon-URL'er (Riots gratis statiske asset-CDN).
/// Champion-billednavne matcher match-v5's interne championName (f.eks.
/// "MonkeyKing" for Wukong), så navnet kan bruges direkte.
/// </summary>
public static class DataDragon
{
    public const string DefaultVersion = "15.13.1";

    public static string ChampionIconUrl(string version, string championName)
    {
        var v = string.IsNullOrWhiteSpace(version) ? DefaultVersion : version.Trim();
        var champ = Uri.EscapeDataString(championName);
        return $"https://ddragon.leagueoflegends.com/cdn/{v}/img/champion/{champ}.png";
    }
}
