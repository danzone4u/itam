using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace itam.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailHtmlTemplate",
                table: "EmailSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailHtmlTemplate",
                table: "EmailSettings");
        }
    }
}
