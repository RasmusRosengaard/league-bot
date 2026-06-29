using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LolMatchAlert.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "last_seen_matches",
                columns: table => new
                {
                    Puuid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LastMatchId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_last_seen_matches", x => x.Puuid);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Puuid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GameName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TagLine = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Region = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DiscordGuildId = table.Column<long>(type: "bigint", nullable: false),
                    DiscordChannelId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_Puuid",
                table: "subscriptions",
                column: "Puuid");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_Puuid_DiscordChannelId",
                table: "subscriptions",
                columns: new[] { "Puuid", "DiscordChannelId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "last_seen_matches");

            migrationBuilder.DropTable(
                name: "subscriptions");
        }
    }
}
