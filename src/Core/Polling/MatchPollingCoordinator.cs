using LolMatchAlert.Core.Riot;
using LolMatchAlert.Core.Subscriptions;
using Microsoft.Extensions.Logging;

namespace LolMatchAlert.Core.Polling;

/// <summary>
/// Poller én konto: henter seneste match-ids, diff'er mod baseline, henter
/// detaljer for de nye kampe og producerer <see cref="MatchAlert"/>'er.
/// Opdaterer sidst-sete match-id så samme kamp aldrig postes to gange.
///
/// Afhænger kun af abstraktioner (IRiotClient, ILastSeenMatchRepository), så hele
/// flowet kan unit-testes uden netværk eller database.
/// </summary>
public sealed class MatchPollingCoordinator(
    IRiotClient riotClient,
    ILastSeenMatchRepository lastSeenRepository,
    ILogger<MatchPollingCoordinator> logger)
{
    public async Task<IReadOnlyList<MatchAlert>> PollAccountAsync(
        AccountToPoll account,
        int matchIdsToFetch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);

        var recentIds = await riotClient.GetRecentMatchIdsAsync(
            account.Puuid, account.Region, matchIdsToFetch, cancellationToken);

        var lastSeen = await lastSeenRepository.GetLastMatchIdAsync(account.Puuid, cancellationToken);
        var diff = MatchDiff.Compute(lastSeen, recentIds);

        var alerts = new List<MatchAlert>();
        foreach (var matchId in diff.NewMatchIdsOldestFirst)
        {
            var match = await riotClient.GetMatchAsync(matchId, account.Region, cancellationToken);
            if (match is null)
            {
                logger.LogWarning("Match {MatchId} for {RiotId} kunne ikke hentes — springes over.", matchId, account.RiotId);
                continue;
            }

            var participant = match.FindParticipant(account.Puuid);
            if (participant is null)
            {
                logger.LogWarning("Deltager {Puuid} ikke fundet i match {MatchId} — springes over.", account.Puuid, matchId);
                continue;
            }

            alerts.Add(MatchAlert.From(account, match, participant));
        }

        // Gem ny baseline (også ved første-syn, hvor intet postes).
        if (!string.IsNullOrEmpty(diff.NewLastSeenMatchId) && diff.NewLastSeenMatchId != lastSeen)
        {
            await lastSeenRepository.SetLastMatchIdAsync(account.Puuid, diff.NewLastSeenMatchId, cancellationToken);
        }

        if (diff.IsFirstSight)
        {
            logger.LogInformation("Første poll af {RiotId} — baseline sat til {MatchId}, ingen historik postet.",
                account.RiotId, diff.NewLastSeenMatchId);
        }

        return alerts;
    }
}
