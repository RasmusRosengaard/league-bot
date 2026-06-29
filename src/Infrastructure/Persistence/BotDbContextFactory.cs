using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LolMatchAlert.Infrastructure.Persistence;

/// <summary>
/// Design-time factory så `dotnet ef migrations` kan oprette BotDbContext uden at
/// starte Bot-hosten. Connection string læses fra miljøvariablen
/// ConnectionStrings__Default (med et lokalt fallback til udvikling).
/// </summary>
public sealed class BotDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
{
    public BotDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=lolmatchalert;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(BotDbContext).Assembly.FullName))
            .Options;

        return new BotDbContext(options);
    }
}
