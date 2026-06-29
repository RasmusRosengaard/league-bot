namespace LolMatchAlert.Core.Polling;

/// <summary>
/// Ren diff-logik: givet det sidst-sete match-id og Riots seneste match-ids
/// (nyeste først), find hvilke kampe der er nye. Helt fri for netværk og I/O,
/// så den kan unit-testes fuldt ud.
/// </summary>
public static class MatchDiff
{
    /// <param name="lastSeenMatchId">Senest behandlede match, eller null hvis kontoen aldrig er pollet.</param>
    /// <param name="recentMatchIdsNewestFirst">Match-ids fra match-v5 i den rækkefølge API'et giver dem (nyeste først).</param>
    public static MatchDiffResult Compute(string? lastSeenMatchId, IReadOnlyList<string> recentMatchIdsNewestFirst)
    {
        ArgumentNullException.ThrowIfNull(recentMatchIdsNewestFirst);

        if (recentMatchIdsNewestFirst.Count == 0)
        {
            // Intet at gå ud fra — bevar nuværende baseline.
            return new MatchDiffResult([], lastSeenMatchId, IsFirstSight: false);
        }

        var newest = recentMatchIdsNewestFirst[0];

        // Første gang vi ser kontoen: post ikke historik, sæt blot baseline.
        if (string.IsNullOrEmpty(lastSeenMatchId))
        {
            return new MatchDiffResult([], newest, IsFirstSight: true);
        }

        // Ingen nye kampe siden sidst.
        if (recentMatchIdsNewestFirst[0] == lastSeenMatchId)
        {
            return new MatchDiffResult([], lastSeenMatchId, IsFirstSight: false);
        }

        var index = IndexOf(recentMatchIdsNewestFirst, lastSeenMatchId);

        // Alt nyere end det sidst-sete (eller hele vinduet hvis vi er kommet bagud).
        var newerCount = index >= 0 ? index : recentMatchIdsNewestFirst.Count;

        // recent er nyeste-først; vend om så de postes ældste-først (kronologisk).
        var newOldestFirst = new List<string>(newerCount);
        for (var i = newerCount - 1; i >= 0; i--)
        {
            newOldestFirst.Add(recentMatchIdsNewestFirst[i]);
        }

        return new MatchDiffResult(newOldestFirst, newest, IsFirstSight: false);
    }

    private static int IndexOf(IReadOnlyList<string> list, string value)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] == value)
            {
                return i;
            }
        }

        return -1;
    }
}

/// <param name="NewMatchIdsOldestFirst">Nye kampe der skal postes, i kronologisk rækkefølge (ældste først).</param>
/// <param name="NewLastSeenMatchId">Den nye baseline der skal gemmes (eller uændret).</param>
/// <param name="IsFirstSight">True hvis dette var første poll af kontoen (kun baseline sat, intet postet).</param>
public sealed record MatchDiffResult(
    IReadOnlyList<string> NewMatchIdsOldestFirst,
    string? NewLastSeenMatchId,
    bool IsFirstSight);
