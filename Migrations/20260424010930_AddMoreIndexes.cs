using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace itam.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BarangSerials_Status",
                table: "BarangSerials",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BarangLokasis_Stok",
                table: "BarangLokasis",
                column: "Stok");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BarangSerials_Status",
                table: "BarangSerials");

            migrationBuilder.DropIndex(
                name: "IX_BarangLokasis_Stok",
                table: "BarangLokasis");
        }
    }
}
