namespace LolMatchAlert.Core.Subscriptions;

public interface ISubscriptionRepository
{
    /// <summary>Tilføj et abonnement. Returnerer false hvis kanalen allerede følger kontoen.</summary>
    Task<bool> AddAsync(Subscription subscription, CancellationToken cancellationToken);

    /// <summary>Fjern abonnementet for et PUUID i en bestemt kanal. Returnerer false hvis intet blev fjernet.</summary>
    Task<bool> RemoveAsync(string puuid, ulong channelId, CancellationToken cancellationToken);

    /// <summary>Findes der allerede et abonnement på kontoen i kanalen?</summary>
    Task<bool> ExistsAsync(string puuid, ulong channelId, CancellationToken cancellationToken);

    /// <summary>Alle abonnementer i en kanal.</summary>
    Task<IReadOnlyList<Subscription>> GetByChannelAsync(ulong channelId, CancellationToken cancellationToken);

    /// <summary>Alle kanaler/abonnementer der følger et bestemt PUUID (til fan-out af nye kampe).</summary>
    Task<IReadOnlyList<Subscription>> GetByPuuidAsync(string puuid, CancellationToken cancellationToken);

    /// <summary>Distinkte konti der skal polles (én pr. unik PUUID+region).</summary>
    Task<IReadOnlyList<AccountToPoll>> GetDistinctAccountsToPollAsync(CancellationToken cancellationToken);
}
