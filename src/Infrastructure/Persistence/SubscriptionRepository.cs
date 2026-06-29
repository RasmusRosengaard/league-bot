using LolMatchAlert.Core.Riot;
using LolMatchAlert.Core.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LolMatchAlert.Infrastructure.Persistence;

public sealed class SubscriptionRepository(BotDbContext db, ILogger<SubscriptionRepository> logger)
    : ISubscriptionRepository
{
    public async Task<bool> AddAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        if (await ExistsAsync(subscription.Puuid, subscription.DiscordChannelId, cancellationToken))
        {
            return false;
        }

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveAsync(string puuid, ulong channelId, CancellationToken cancellationToken)
    {
        var channel = (long)channelId;
        var deleted = await db.Subscriptions
            .Where(s => s.Puuid == puuid && EF.Property<long>(s, nameof(Subscription.DiscordChannelId)) == channel)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    public Task<bool> ExistsAsync(string puuid, ulong channelId, CancellationToken cancellationToken)
    {
        var channel = (long)channelId;
        return db.Subscriptions
            .AnyAsync(s => s.Puuid == puuid
                && EF.Property<long>(s, nameof(Subscription.DiscordChannelId)) == channel,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetByChannelAsync(ulong channelId, CancellationToken cancellationToken)
    {
        var channel = (long)channelId;
        return await db.Subscriptions
            .Where(s => EF.Property<long>(s, nameof(Subscription.DiscordChannelId)) == channel)
            .OrderBy(s => s.GameName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetByPuuidAsync(string puuid, CancellationToken cancellationToken)
    {
        return await db.Subscriptions
            .Where(s => s.Puuid == puuid)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountToPoll>> GetDistinctAccountsToPollAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Subscriptions
            .Select(s => new { s.Puuid, s.GameName, s.TagLine, s.Region })
            .Distinct()
            .ToListAsync(cancellationToken);

        var accounts = new List<AccountToPoll>();
        foreach (var group in rows.GroupBy(r => r.Puuid))
        {
            var row = group.First();
            if (!Regions.TryParse(row.Region, out var region))
            {
                logger.LogWarning("Springer konto {Puuid} over: ukendt region '{Region}'.", row.Puuid, row.Region);
                continue;
            }

            accounts.Add(new AccountToPoll(row.Puuid, row.GameName, row.TagLine, region));
        }

        return accounts;
    }
}
