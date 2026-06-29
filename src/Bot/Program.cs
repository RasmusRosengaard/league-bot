using LolMatchAlert.Bot.Discord;
using LolMatchAlert.Bot.Polling;
using LolMatchAlert.Infrastructure.Persistence;
using LolMatchAlert.Infrastructure.Riot;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Struktureret logging via det indbyggede ILogger-framework.
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

// Riot-klient (resilience), persistering (Postgres + repositories), Discord-bot.
builder.Services.AddRiotClient(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddDiscordBot(builder.Configuration);
builder.Services.AddPolling(builder.Configuration);

var host = builder.Build();

// Kør EF-migrationer ved opstart (kan slås fra med Database:AutoMigrate=false,
// f.eks. hvis migrationer køres som et separat deploy-trin).
if (builder.Configuration.GetValue("Database:AutoMigrate", defaultValue: true))
{
    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
