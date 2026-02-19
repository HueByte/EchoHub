using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoHub.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelMemberships",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChannelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JoinedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMemberships", x => new { x.UserId, x.ChannelId });
                    table.ForeignKey(
                        name: "FK_ChannelMemberships_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMemberships_ChannelId",
                table: "ChannelMemberships",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMemberships_UserId",
                table: "ChannelMemberships",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelMemberships");
        }
    }
}
