namespace LolMatchAlert.Core.Riot;

/// <summary>
/// Abstraktion over de Riot-endpoints botten bruger. Lever bag et interface så
/// poll-/diff-logikken kan unit-testes uden netværk, og så implementationen kan
/// mockes mod WireMock i integrationstests.
/// </summary>
public interface IRiotClient
{
    /// <summary>
    /// account-v1: oversæt et Riot-ID til en konto (PUUID).
    /// Returnerer null hvis kontoen ikke findes (404).
    /// </summary>
    Task<RiotAccount?> GetAccountByRiotIdAsync(RiotId riotId, Region region, CancellationToken cancellationToken);

    /// <summary>
    /// match-v5: hent de seneste match-ids for en PUUID (nyeste først).
    /// </summary>
    Task<IReadOnlyList<string>> GetRecentMatchIdsAsync(string puuid, Region region, int count, CancellationToken cancellationToken);

    /// <summary>
    /// match-v5: hent detaljer for ét match. Returnerer null hvis matchet ikke findes (404).
    /// </summary>
    Task<MatchDetail?> GetMatchAsync(string matchId, Region region, CancellationToken cancellationToken);
}
