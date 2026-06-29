using LolMatchAlert.Core.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace LolMatchAlert.Infrastructure.Persistence;

public sealed class LastSeenMatchRepository(BotDbContext db, TimeProvider timeProvider)
    : ILastSeenMatchRepository
{
    public async Task<string?> GetLastMatchIdAsync(string puuid, CancellationToken cancellationToken)
    {
        return await db.LastSeenMatches
            .Where(m => m.Puuid == puuid)
            .Select(m => m.LastMatchId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SetLastMatchIdAsync(string puuid, string matchId, CancellationToken cancellationToken)
    {
        var existing = await db.LastSeenMatches.FindAsync([puuid], cancellationToken);
        if (existing is null)
        {
            db.LastSeenMatches.Add(new LastSeenMatch
            {
                Puuid = puuid,
                LastMatchId = matchId,
                UpdatedAt = timeProvider.GetUtcNow(),
            });
        }
        else
        {
            existing.LastMatchId = matchId;
            existing.UpdatedAt = timeProvider.GetUtcNow();
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
