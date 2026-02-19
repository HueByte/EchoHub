using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoHub.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelIsPublic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Channels",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Channels");
        }
    }
}
