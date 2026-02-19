using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoHub.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageEmbed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmbedJson",
                table: "Messages",
                type: "TEXT",
                maxLength: 8000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbedJson",
                table: "Messages");
        }
    }
}
