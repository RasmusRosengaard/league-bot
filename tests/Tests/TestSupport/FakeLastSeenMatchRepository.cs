using LolMatchAlert.Core.Subscriptions;

namespace LolMatchAlert.Tests.TestSupport;

/// <summary>In-memory fake af ILastSeenMatchRepository.</summary>
internal sealed class FakeLastSeenMatchRepository : ILastSeenMatchRepository
{
    public Dictionary<string, string> Store { get; } = new();

    public Task<string?> GetLastMatchIdAsync(string puuid, CancellationToken cancellationToken)
        => Task.FromResult(Store.GetValueOrDefault(puuid));

    public Task SetLastMatchIdAsync(string puuid, string matchId, CancellationToken cancellationToken)
    {
        Store[puuid] = matchId;
        return Task.CompletedTask;
    }
}
