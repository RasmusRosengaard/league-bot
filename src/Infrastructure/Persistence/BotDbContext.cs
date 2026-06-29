using LolMatchAlert.Core.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace LolMatchAlert.Infrastructure.Persistence;

public sealed class BotDbContext(DbContextOptions<BotDbContext> options) : DbContext(options)
{
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<LastSeenMatch> LastSeenMatches => Set<LastSeenMatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions");
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Puuid).IsRequired().HasMaxLength(128);
            entity.Property(s => s.GameName).IsRequired().HasMaxLength(64);
            entity.Property(s => s.TagLine).IsRequired().HasMaxLength(16);
            entity.Property(s => s.Region).IsRequired().HasMaxLength(16);

            // Discord-snowflakes (ulong) gemmes som bigint. Snowflakes ligger godt
            // inden for long-rækkevidde i mange årtier endnu.
            entity.Property(s => s.DiscordGuildId).HasConversion<long>();
            entity.Property(s => s.DiscordChannelId).HasConversion<long>();

            entity.Property(s => s.CreatedAt);

            // Samme konto må kun følges én gang pr. kanal.
            entity.HasIndex(s => new { s.Puuid, s.DiscordChannelId }).IsUnique();
            // Hurtige opslag ved fan-out.
            entity.HasIndex(s => s.Puuid);
        });

        modelBuilder.Entity<LastSeenMatch>(entity =>
        {
            entity.ToTable("last_seen_matches");
            entity.HasKey(m => m.Puuid);

            entity.Property(m => m.Puuid).HasMaxLength(128);
            entity.Property(m => m.LastMatchId).IsRequired().HasMaxLength(32);
            entity.Property(m => m.UpdatedAt);
        });
    }
}
