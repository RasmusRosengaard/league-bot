namespace LolMatchAlert.Core.Subscriptions;

public interface ILastSeenMatchRepository
{
    /// <summary>Sidst-sete match-id for et PUUID, eller null hvis kontoen aldrig er pollet.</summary>
    Task<string?> GetLastMatchIdAsync(string puuid, CancellationToken cancellationToken);

    /// <summary>Opdatér (eller indsæt) sidst-sete match-id for et PUUID.</summary>
    Task SetLastMatchIdAsync(string puuid, string matchId, CancellationToken cancellationToken);
}
