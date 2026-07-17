using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoHub.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServerStatsReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServerStatsReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GeneratedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    PeriodStart = table.Column<long>(type: "INTEGER", nullable: false),
                    PeriodEnd = table.Column<long>(type: "INTEGER", nullable: false),
                    WindowHours = table.Column<double>(type: "REAL", nullable: false),
                    MessagesSent = table.Column<int>(type: "INTEGER", nullable: false),
                    FilesUploaded = table.Column<int>(type: "INTEGER", nullable: false),
                    BytesUploaded = table.Column<long>(type: "INTEGER", nullable: false),
                    NewMembers = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveMembers = table.Column<int>(type: "INTEGER", nullable: false),
                    Connections = table.Column<int>(type: "INTEGER", nullable: false),
                    Disconnections = table.Column<int>(type: "INTEGER", nullable: false),
                    Kicks = table.Column<int>(type: "INTEGER", nullable: false),
                    Bans = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalMembers = table.Column<int>(type: "INTEGER", nullable: false),
                    OnlineNow = table.Column<int>(type: "INTEGER", nullable: false),
                    PeakOnline = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerStatsReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerStatsReports_GeneratedAt",
                table: "ServerStatsReports",
                column: "GeneratedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerStatsReports");
        }
    }
}
