using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace itam.Migrations
{
    /// <inheritdoc />
    public partial class AddBarangSerialToPeminjaman : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BarangSerialId",
                table: "Peminjamans",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Peminjamans_BarangSerialId",
                table: "Peminjamans",
                column: "BarangSerialId");

            migrationBuilder.AddForeignKey(
                name: "FK_Peminjamans_BarangSerials_BarangSerialId",
                table: "Peminjamans",
                column: "BarangSerialId",
                principalTable: "BarangSerials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Peminjamans_BarangSerials_BarangSerialId",
                table: "Peminjamans");

            migrationBuilder.DropIndex(
                name: "IX_Peminjamans_BarangSerialId",
                table: "Peminjamans");

            migrationBuilder.DropColumn(
                name: "BarangSerialId",
                table: "Peminjamans");
        }
    }
}
