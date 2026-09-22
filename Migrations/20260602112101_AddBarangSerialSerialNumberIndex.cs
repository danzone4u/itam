using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace itam.Migrations
{
    /// <inheritdoc />
    public partial class AddBarangSerialSerialNumberIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BarangSerials_SerialNumber",
                table: "BarangSerials",
                column: "SerialNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BarangSerials_SerialNumber",
                table: "BarangSerials");
        }
    }
}
