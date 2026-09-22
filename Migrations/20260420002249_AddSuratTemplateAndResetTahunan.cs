using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace itam.Migrations
{
    /// <inheritdoc />
    public partial class AddSuratTemplateAndResetTahunan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ResetTahunan",
                table: "SuratSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TemplateSuratJalan",
                table: "SuratSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemplateSuratKembali",
                table: "SuratSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemplateSuratPeminjaman",
                table: "SuratSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemplateSuratTerima",
                table: "SuratSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResetTahunan",
                table: "SuratSettings");

            migrationBuilder.DropColumn(
                name: "TemplateSuratJalan",
                table: "SuratSettings");

            migrationBuilder.DropColumn(
                name: "TemplateSuratKembali",
                table: "SuratSettings");

            migrationBuilder.DropColumn(
                name: "TemplateSuratPeminjaman",
                table: "SuratSettings");

            migrationBuilder.DropColumn(
                name: "TemplateSuratTerima",
                table: "SuratSettings");
        }
    }
}
