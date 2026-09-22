using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace itam.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LogoPath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FaviconPath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Arsips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NamaDokumen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NomorDokumen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JenisDokumen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NamaFile = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Keterangan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arsips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackupSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AutoBackupEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IntervalHours = table.Column<int>(type: "int", nullable: false),
                    LastBackupAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BackupPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChartSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NamaChart = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipeChart = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SumberData = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JumlahBulan = table.Column<int>(type: "int", nullable: false),
                    WarnaUtama = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    WarnaKedua = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    Urutan = table.Column<int>(type: "int", nullable: false),
                    Lebar = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kategoris",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NamaKategori = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KodePrefix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Deskripsi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategoris", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KopSurats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NamaPerusahaan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SubJudul = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Alamat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Telepon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NamaPengirim = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    JabatanPengirim = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TampilkanLogo = table.Column<bool>(type: "bit", nullable: false),
                    LogoPath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KopSurats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lokasis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NamaLokasi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Alamat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PenanggungJawab = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NoTelp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lokasis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StokOpnames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TanggalOpname = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Keterangan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokOpnames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NamaSupplier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Alamat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Telepon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuratSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrefixSuratJalan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrefixSuratKembali = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrefixSuratTerima = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PrefixSuratPeminjaman = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FormatTanggal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PanjangNomorUrut = table.Column<int>(type: "int", nullable: false),
                    Separator = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResetBulanan = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuratSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Barangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KodeBarang = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NamaBarang = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    KategoriId = table.Column<int>(type: "int", nullable: false),
                    Satuan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Stok = table.Column<int>(type: "int", nullable: false),
                    StokMinimum = table.Column<int>(type: "int", nullable: false),
                    Gambar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Deskripsi = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Barangs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Barangs_Kategoris_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "Kategoris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BarangKeluars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarangId = table.Column<int>(type: "int", nullable: false),
                    Jumlah = table.Column<int>(type: "int", nullable: false),
                    TanggalKeluar = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Penerima = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Alamat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NoHpPenerima = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Keterangan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Pic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NoSuratJalan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LokasiId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarangKeluars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BarangKeluars_Barangs_BarangId",
                        column: x => x.BarangId,
                        principalTable: "Barangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BarangKeluars_Lokasis_LokasiId",
                        column: x => x.LokasiId,
                        principalTable: "Lokasis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BarangLokasis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarangId = table.Column<int>(type: "int", nullable: false),
                    LokasiId = table.Column<int>(type: "int", nullable: false),
                    Stok = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarangLokasis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BarangLokasis_Barangs_BarangId",
                        column: x => x.BarangId,
                        principalTable: "Barangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BarangLokasis_Lokasis_LokasiId",
                        column: x => x.LokasiId,
                        principalTable: "Lokasis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BarangMasuks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarangId = table.Column<int>(type: "int", nullable: false),
                    Jumlah = table.Column<int>(type: "int", nullable: false),
                    TanggalMasuk = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HargaSatuan = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Keterangan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LokasiId = table.Column<int>(type: "int", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarangMasuks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BarangMasuks_Barangs_BarangId",
                        column: x => x.BarangId,
                        principalTable: "Barangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BarangMasuks_Lokasis_LokasiId",
                        column: x => x.LokasiId,
                        principalTable: "Lokasis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BarangMasuks_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Peminjamans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarangId = table.Column<int>(type: "int", nullable: false),
                    Jumlah = table.Column<int>(type: "int", nullable: false),
                    Peminjam = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NipNik = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Departemen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NoHp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TanggalPinjam = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TanggalJatuhTempo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TanggalKembali = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    KondisiKembali = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NoPeminjaman = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Keterangan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Peminjamans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Peminjamans_Barangs_BarangId",
                        column: x => x.BarangId,
                        principalTable: "Barangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StokOpnameDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StokOpnameId = table.Column<int>(type: "int", nullable: false),
                    BarangId = table.Column<int>(type: "int", nullable: false),
                    StokSistem = table.Column<int>(type: "int", nullable: false),
                    StokFisik = table.Column<int>(type: "int", nullable: false),
                    Selisih = table.Column<int>(type: "int", nullable: false),
                    Keterangan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokOpnameDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokOpnameDetails_Barangs_BarangId",
                        column: x => x.BarangId,
                        principalTable: "Barangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokOpnameDetails_StokOpnames_StokOpnameId",
                        column: x => x.StokOpnameId,
                        principalTable: "StokOpnames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransferBarangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarangId = table.Column<int>(type: "int", nullable: false),
                    DariLokasiId = table.Column<int>(type: "int", nullable: false),
                    KeLokasiId = table.Column<int>(type: "int", nullable: false),
                    Jumlah = table.Column<int>(type: "int", nullable: false),
                    TanggalTransfer = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Keterangan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NoTransfer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferBarangs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferBarangs_Barangs_BarangId",
                        column: x => x.BarangId,
                        principalTable: "Barangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferBarangs_Lokasis_DariLokasiId",
                        column: x => x.DariLokasiId,
                        principalTable: "Lokasis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferBarangs_Lokasis_KeLokasiId",
                        column: x => x.KeLokasiId,
                        principalTable: "Lokasis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BarangKembalis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarangId = table.Column<int>(type: "int", nullable: false),
                    BarangKeluarId = table.Column<int>(type: "int", nullable: true),
                    Jumlah = table.Column<int>(type: "int", nullable: false),
                    TanggalKembali = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Kondisi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DikembalikanOleh = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Keterangan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TindakLanjut = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarangKembalis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BarangKembalis_BarangKeluars_BarangKeluarId",
                        column: x => x.BarangKeluarId,
                        principalTable: "BarangKeluars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BarangKembalis_Barangs_BarangId",
                        column: x => x.BarangId,
                        principalTable: "Barangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BarangSerials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarangId = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BarangMasukId = table.Column<int>(type: "int", nullable: true),
                    BarangKeluarId = table.Column<int>(type: "int", nullable: true),
                    Kondisi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BarangKembaliId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarangSerials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BarangSerials_BarangKeluars_BarangKeluarId",
                        column: x => x.BarangKeluarId,
                        principalTable: "BarangKeluars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BarangSerials_BarangKembalis_BarangKembaliId",
                        column: x => x.BarangKembaliId,
                        principalTable: "BarangKembalis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BarangSerials_BarangMasuks_BarangMasukId",
                        column: x => x.BarangMasukId,
                        principalTable: "BarangMasuks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BarangSerials_Barangs_BarangId",
                        column: x => x.BarangId,
                        principalTable: "Barangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferBarangSerials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferBarangId = table.Column<int>(type: "int", nullable: false),
                    BarangSerialId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferBarangSerials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferBarangSerials_BarangSerials_BarangSerialId",
                        column: x => x.BarangSerialId,
                        principalTable: "BarangSerials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferBarangSerials_TransferBarangs_TransferBarangId",
                        column: x => x.TransferBarangId,
                        principalTable: "TransferBarangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BarangKeluars_BarangId",
                table: "BarangKeluars",
                column: "BarangId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangKeluars_LokasiId",
                table: "BarangKeluars",
                column: "LokasiId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangKembalis_BarangId",
                table: "BarangKembalis",
                column: "BarangId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangKembalis_BarangKeluarId",
                table: "BarangKembalis",
                column: "BarangKeluarId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangLokasis_BarangId",
                table: "BarangLokasis",
                column: "BarangId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangLokasis_LokasiId",
                table: "BarangLokasis",
                column: "LokasiId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangMasuks_BarangId",
                table: "BarangMasuks",
                column: "BarangId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangMasuks_LokasiId",
                table: "BarangMasuks",
                column: "LokasiId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangMasuks_SupplierId",
                table: "BarangMasuks",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Barangs_KategoriId",
                table: "Barangs",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangSerials_BarangId",
                table: "BarangSerials",
                column: "BarangId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangSerials_BarangKeluarId",
                table: "BarangSerials",
                column: "BarangKeluarId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangSerials_BarangKembaliId",
                table: "BarangSerials",
                column: "BarangKembaliId");

            migrationBuilder.CreateIndex(
                name: "IX_BarangSerials_BarangMasukId",
                table: "BarangSerials",
                column: "BarangMasukId");

            migrationBuilder.CreateIndex(
                name: "IX_Peminjamans_BarangId",
                table: "Peminjamans",
                column: "BarangId");

            migrationBuilder.CreateIndex(
                name: "IX_StokOpnameDetails_BarangId",
                table: "StokOpnameDetails",
                column: "BarangId");

            migrationBuilder.CreateIndex(
                name: "IX_StokOpnameDetails_StokOpnameId",
                table: "StokOpnameDetails",
                column: "StokOpnameId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferBarangs_BarangId",
                table: "TransferBarangs",
                column: "BarangId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferBarangs_DariLokasiId",
                table: "TransferBarangs",
                column: "DariLokasiId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferBarangs_KeLokasiId",
                table: "TransferBarangs",
                column: "KeLokasiId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferBarangSerials_BarangSerialId",
                table: "TransferBarangSerials",
                column: "BarangSerialId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferBarangSerials_TransferBarangId",
                table: "TransferBarangSerials",
                column: "TransferBarangId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "Arsips");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BackupSettings");

            migrationBuilder.DropTable(
                name: "BarangLokasis");

            migrationBuilder.DropTable(
                name: "ChartSettings");

            migrationBuilder.DropTable(
                name: "KopSurats");

            migrationBuilder.DropTable(
                name: "Peminjamans");

            migrationBuilder.DropTable(
                name: "StokOpnameDetails");

            migrationBuilder.DropTable(
                name: "SuratSettings");

            migrationBuilder.DropTable(
                name: "TransferBarangSerials");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "StokOpnames");

            migrationBuilder.DropTable(
                name: "BarangSerials");

            migrationBuilder.DropTable(
                name: "TransferBarangs");

            migrationBuilder.DropTable(
                name: "BarangKembalis");

            migrationBuilder.DropTable(
                name: "BarangMasuks");

            migrationBuilder.DropTable(
                name: "BarangKeluars");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Barangs");

            migrationBuilder.DropTable(
                name: "Lokasis");

            migrationBuilder.DropTable(
                name: "Kategoris");
        }
    }
}
