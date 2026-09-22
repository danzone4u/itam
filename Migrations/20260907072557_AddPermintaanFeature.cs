using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace itam.Migrations
{
    /// <inheritdoc />
    public partial class AddPermintaanFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BarangSerials_SerialNumber",
                table: "BarangSerials");

            migrationBuilder.CreateTable(
                name: "Permintaans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NoPermintaan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JenisPermintaan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PemohonUser = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Penerima = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NipNik = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Departemen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NoHp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LokasiId = table.Column<int>(type: "int", nullable: true),
                    Keperluan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TanggalPengajuan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TanggalDibutuhkan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CatatanAdmin = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BarangKeluarId = table.Column<int>(type: "int", nullable: true),
                    PeminjamanId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permintaans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permintaans_BarangKeluars_BarangKeluarId",
                        column: x => x.BarangKeluarId,
                        principalTable: "BarangKeluars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Permintaans_Lokasis_LokasiId",
                        column: x => x.LokasiId,
                        principalTable: "Lokasis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Permintaans_Peminjamans_PeminjamanId",
                        column: x => x.PeminjamanId,
                        principalTable: "Peminjamans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermintaanDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermintaanId = table.Column<int>(type: "int", nullable: false),
                    BarangId = table.Column<int>(type: "int", nullable: false),
                    Jumlah = table.Column<int>(type: "int", nullable: false),
                    Catatan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermintaanDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermintaanDetails_Barangs_BarangId",
                        column: x => x.BarangId,
                        principalTable: "Barangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermintaanDetails_Permintaans_PermintaanId",
                        column: x => x.PermintaanId,
                        principalTable: "Permintaans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermintaanDetails_BarangId",
                table: "PermintaanDetails",
                column: "BarangId");

            migrationBuilder.CreateIndex(
                name: "IX_PermintaanDetails_PermintaanId",
                table: "PermintaanDetails",
                column: "PermintaanId");

            migrationBuilder.CreateIndex(
                name: "IX_Permintaans_BarangKeluarId",
                table: "Permintaans",
                column: "BarangKeluarId");

            migrationBuilder.CreateIndex(
                name: "IX_Permintaans_LokasiId",
                table: "Permintaans",
                column: "LokasiId");

            migrationBuilder.CreateIndex(
                name: "IX_Permintaans_PeminjamanId",
                table: "Permintaans",
                column: "PeminjamanId");

            migrationBuilder.CreateIndex(
                name: "IX_Permintaans_PemohonUser",
                table: "Permintaans",
                column: "PemohonUser");

            migrationBuilder.CreateIndex(
                name: "IX_Permintaans_Status",
                table: "Permintaans",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermintaanDetails");

            migrationBuilder.DropTable(
                name: "Permintaans");

            migrationBuilder.CreateIndex(
                name: "IX_BarangSerials_SerialNumber",
                table: "BarangSerials",
                column: "SerialNumber");
        }
    }
}
