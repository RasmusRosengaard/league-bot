using LolMatchAlert.Core.Riot;

namespace LolMatchAlert.Tests.TestSupport;

/// <summary>In-memory fake af IRiotClient til netværksfri unit-tests af poll-logikken.</summary>
internal sealed class FakeRiotClient : IRiotClient
{
    public Dictionary<string, IReadOnlyList<string>> MatchIdsByPuuid { get; } = new();
    public Dictionary<string, MatchDetail> MatchesById { get; } = new();
    public List<string> RequestedMatchIds { get; } = [];

    public Task<RiotAccount?> GetAccountByRiotIdAsync(RiotId riotId, Region region, CancellationToken cancellationToken)
        => Task.FromResult<RiotAccount?>(new RiotAccount("puuid-" + riotId.GameName, riotId.GameName, riotId.TagLine));

    public Task<IReadOnlyList<string>> GetRecentMatchIdsAsync(string puuid, Region region, int count, CancellationToken cancellationToken)
        => Task.FromResult(MatchIdsByPuuid.TryGetValue(puuid, out var ids) ? ids : (IReadOnlyList<string>)[]);

    public Task<MatchDetail?> GetMatchAsync(string matchId, Region region, CancellationToken cancellationToken)
    {
        RequestedMatchIds.Add(matchId);
        return Task.FromResult(MatchesById.GetValueOrDefault(matchId));
    }
}
