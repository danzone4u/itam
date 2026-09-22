using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace itam.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Barangs_KodeBarang",
                table: "Barangs",
                column: "KodeBarang");

            migrationBuilder.CreateIndex(
                name: "IX_Barangs_NamaBarang",
                table: "Barangs",
                column: "NamaBarang");

            migrationBuilder.CreateIndex(
                name: "IX_BarangMasuks_TanggalMasuk",
                table: "BarangMasuks",
                column: "TanggalMasuk");

            migrationBuilder.CreateIndex(
                name: "IX_BarangKeluars_TanggalKeluar",
                table: "BarangKeluars",
                column: "TanggalKeluar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Barangs_KodeBarang",
                table: "Barangs");

            migrationBuilder.DropIndex(
                name: "IX_Barangs_NamaBarang",
                table: "Barangs");

            migrationBuilder.DropIndex(
                name: "IX_BarangMasuks_TanggalMasuk",
                table: "BarangMasuks");

            migrationBuilder.DropIndex(
                name: "IX_BarangKeluars_TanggalKeluar",
                table: "BarangKeluars");
        }
    }
}
