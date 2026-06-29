namespace LolMatchAlert.Core.Riot;

/// <summary>
/// Regional routing-cluster brugt af account-v1 og match-v5.
/// Bemærk: dette er IKKE det samme som platform-routing (euw1, na1, kr ...),
/// som bruges af f.eks. summoner-v4. Account-v1 og match-v5 bruger udelukkende
/// disse regionale clustre.
/// </summary>
public enum RegionalRoute
{
    Americas,
    Asia,
    Europe,
    Sea,
}
