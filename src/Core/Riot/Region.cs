namespace LolMatchAlert.Core.Riot;

/// <summary>
/// En brugervendt region (f.eks. "euw1") parret med det regionale routing-cluster
/// account-v1 og match-v5 skal kaldes på.
/// </summary>
/// <param name="Platform">Kanonisk platform-id som gemmes/visning (f.eks. "euw1").</param>
/// <param name="Route">Det regionale cluster kald skal sendes til.</param>
public sealed record Region(string Platform, RegionalRoute Route)
{
    /// <summary>Hostnavn-fragment til regional routing, f.eks. "europe".</summary>
    public string RegionalHost => Route switch
    {
        RegionalRoute.Americas => "americas",
        RegionalRoute.Asia => "asia",
        RegionalRoute.Europe => "europe",
        RegionalRoute.Sea => "sea",
        _ => throw new ArgumentOutOfRangeException(nameof(Route), Route, "Ukendt regional route."),
    };
}
