using LolMatchAlert.Core.Subscriptions;
using LolMatchAlert.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace LolMatchAlert.Tests.Persistence;

/// <summary>
/// Integrationstest mod en rigtig PostgreSQL (Testcontainers). Verificerer at
/// EF-migrationer kører, og at abonnement-/idempotens-logikken virker.
/// Kræver at Docker kører.
/// </summary>
public sealed class PersistenceIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Kør migrationer (ikke EnsureCreated) — samme vej som produktion.
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private BotDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new BotDbContext(options);
    }

    private static Subscription NewSub(string puuid, ulong channel, ulong guild = 1, string region = "euw1") => new()
    {
        Id = Guid.NewGuid(),
        Puuid = puuid,
        GameName = "Player" + puuid[..3],
        TagLine = "EUW",
        Region = region,
        DiscordGuildId = guild,
        DiscordChannelId = channel,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Migrationer_koerer_og_skema_er_brugbart()
    {
        await using var db = CreateContext();

        // Hvis tabellerne ikke fandtes ville disse kald kaste.
        Assert.True(await db.Database.CanConnectAsync());
        Assert.Equal(0, await db.Subscriptions.CountAsync(s => s.Puuid == "nonexistent"));
        Assert.Equal(0, await db.LastSeenMatches.CountAsync(m => m.Puuid == "nonexistent"));
    }

    [Fact]
    public async Task Samme_konto_i_to_kanaler_giver_ét_poll_men_to_fanout_maal()
    {
        await using var db = CreateContext();
        var repo = new SubscriptionRepository(db, NullLogger<SubscriptionRepository>.Instance);
        const string puuid = "puuid-shared-001";

        Assert.True(await repo.AddAsync(NewSub(puuid, channel: 1001), CancellationToken.None));
        Assert.True(await repo.AddAsync(NewSub(puuid, channel: 1002), CancellationToken.None));

        // Idempotens: poll-listen dedupes på PUUID -> netop én konto at polle.
        var accounts = await repo.GetDistinctAccountsToPollAsync(CancellationToken.None);
        var shared = accounts.Where(a => a.Puuid == puuid).ToList();
        Assert.Single(shared);
        Assert.Equal(RegionalRouteEurope, shared[0].Region.RegionalHost);

        // Fan-out: begge kanaler skal modtage opslag.
        var targets = await repo.GetByPuuidAsync(puuid, CancellationToken.None);
        Assert.Equal(2, targets.Count);
    }

    [Fact]
    public async Task Dublet_abonnement_i_samme_kanal_afvises()
    {
        await using var db = CreateContext();
        var repo = new SubscriptionRepository(db, NullLogger<SubscriptionRepository>.Instance);
        const string puuid = "puuid-dupe-002";

        Assert.True(await repo.AddAsync(NewSub(puuid, channel: 2001), CancellationToken.None));
        Assert.False(await repo.AddAsync(NewSub(puuid, channel: 2001), CancellationToken.None));
        Assert.True(await repo.ExistsAsync(puuid, 2001, CancellationToken.None));

        var inChannel = await repo.GetByChannelAsync(2001, CancellationToken.None);
        Assert.Single(inChannel);
    }

    [Fact]
    public async Task Unsubscribe_fjerner_kun_i_den_kanal()
    {
        await using var db = CreateContext();
        var repo = new SubscriptionRepository(db, NullLogger<SubscriptionRepository>.Instance);
        const string puuid = "puuid-remove-003";

        await repo.AddAsync(NewSub(puuid, channel: 3001), CancellationToken.None);
        await repo.AddAsync(NewSub(puuid, channel: 3002), CancellationToken.None);

        Assert.True(await repo.RemoveAsync(puuid, 3001, CancellationToken.None));
        Assert.False(await repo.RemoveAsync(puuid, 3001, CancellationToken.None)); // allerede væk

        var remaining = await repo.GetByPuuidAsync(puuid, CancellationToken.None);
        Assert.Single(remaining);
        Assert.Equal(3002ul, remaining[0].DiscordChannelId);
    }

    [Fact]
    public async Task LastSeenMatch_upsert_virker()
    {
        await using var db = CreateContext();
        var repo = new LastSeenMatchRepository(db, TimeProvider.System);
        const string puuid = "puuid-lsm-004";

        Assert.Null(await repo.GetLastMatchIdAsync(puuid, CancellationToken.None));

        await repo.SetLastMatchIdAsync(puuid, "EUW1_111", CancellationToken.None);
        Assert.Equal("EUW1_111", await repo.GetLastMatchIdAsync(puuid, CancellationToken.None));

        await repo.SetLastMatchIdAsync(puuid, "EUW1_222", CancellationToken.None);
        Assert.Equal("EUW1_222", await repo.GetLastMatchIdAsync(puuid, CancellationToken.None));
    }

    private const string RegionalRouteEurope = "europe";
}
