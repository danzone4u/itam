using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace itam.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOperasionalToBarang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOperasional",
                table: "Barangs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Barangs_IsOperasional",
                table: "Barangs",
                column: "IsOperasional");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Barangs_IsOperasional",
                table: "Barangs");

            migrationBuilder.DropColumn(
                name: "IsOperasional",
                table: "Barangs");
        }
    }
}
