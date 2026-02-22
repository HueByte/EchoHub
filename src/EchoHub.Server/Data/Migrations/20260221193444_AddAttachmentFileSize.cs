using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EchoHub.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentFileSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AttachmentFileSize",
                table: "Messages",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentFileSize",
                table: "Messages");
        }
    }
}
