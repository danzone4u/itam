IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ActivityLogs] (
    [Id] int NOT NULL IDENTITY,
    [UserName] nvarchar(200) NOT NULL,
    [Action] nvarchar(100) NOT NULL,
    [Module] nvarchar(100) NOT NULL,
    [Detail] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IpAddress] nvarchar(50) NULL,
    CONSTRAINT [PK_ActivityLogs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AppSettings] (
    [Id] int NOT NULL IDENTITY,
    [AppName] nvarchar(100) NOT NULL,
    [LogoPath] nvarchar(255) NULL,
    [FaviconPath] nvarchar(255) NULL,
    CONSTRAINT [PK_AppSettings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Arsips] (
    [Id] int NOT NULL IDENTITY,
    [NamaDokumen] nvarchar(200) NOT NULL,
    [NomorDokumen] nvarchar(100) NULL,
    [JenisDokumen] nvarchar(100) NULL,
    [FilePath] nvarchar(500) NULL,
    [NamaFile] nvarchar(300) NULL,
    [Keterangan] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Arsips] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [BackupSettings] (
    [Id] int NOT NULL IDENTITY,
    [AutoBackupEnabled] bit NOT NULL,
    [IntervalHours] int NOT NULL,
    [LastBackupAt] datetime2 NULL,
    [BackupPath] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_BackupSettings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ChartSettings] (
    [Id] int NOT NULL IDENTITY,
    [NamaChart] nvarchar(100) NOT NULL,
    [TipeChart] nvarchar(30) NOT NULL,
    [SumberData] nvarchar(50) NOT NULL,
    [JumlahBulan] int NOT NULL,
    [WarnaUtama] nvarchar(20) NULL,
    [WarnaKedua] nvarchar(20) NULL,
    [Aktif] bit NOT NULL,
    [Urutan] int NOT NULL,
    [Lebar] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ChartSettings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Kategoris] (
    [Id] int NOT NULL IDENTITY,
    [NamaKategori] nvarchar(100) NOT NULL,
    [KodePrefix] nvarchar(10) NULL,
    [Deskripsi] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Kategoris] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [KopSurats] (
    [Id] int NOT NULL IDENTITY,
    [NamaPerusahaan] nvarchar(200) NOT NULL,
    [SubJudul] nvarchar(200) NULL,
    [Alamat] nvarchar(500) NULL,
    [Telepon] nvarchar(50) NULL,
    [Email] nvarchar(100) NULL,
    [Website] nvarchar(200) NULL,
    [NamaPengirim] nvarchar(200) NULL,
    [JabatanPengirim] nvarchar(200) NULL,
    [TampilkanLogo] bit NOT NULL,
    [LogoPath] nvarchar(255) NULL,
    CONSTRAINT [PK_KopSurats] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Lokasis] (
    [Id] int NOT NULL IDENTITY,
    [Kode] nvarchar(20) NOT NULL,
    [NamaLokasi] nvarchar(200) NOT NULL,
    [Alamat] nvarchar(500) NULL,
    [PenanggungJawab] nvarchar(200) NULL,
    [NoTelp] nvarchar(20) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Lokasis] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [StokOpnames] (
    [Id] int NOT NULL IDENTITY,
    [TanggalOpname] datetime2 NOT NULL,
    [Keterangan] nvarchar(500) NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StokOpnames] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Suppliers] (
    [Id] int NOT NULL IDENTITY,
    [NamaSupplier] nvarchar(200) NOT NULL,
    [Alamat] nvarchar(500) NULL,
    [Telepon] nvarchar(20) NULL,
    [Email] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [SuratSettings] (
    [Id] int NOT NULL IDENTITY,
    [PrefixSuratJalan] nvarchar(50) NOT NULL,
    [PrefixSuratKembali] nvarchar(50) NOT NULL,
    [PrefixSuratTerima] nvarchar(50) NOT NULL,
    [PrefixSuratPeminjaman] nvarchar(50) NOT NULL,
    [FormatTanggal] nvarchar(20) NOT NULL,
    [PanjangNomorUrut] int NOT NULL,
    [Separator] nvarchar(5) NOT NULL,
    [Suffix] nvarchar(50) NULL,
    [ResetBulanan] bit NOT NULL,
    CONSTRAINT [PK_SuratSettings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(128) NOT NULL,
    [ProviderKey] nvarchar(128) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(128) NOT NULL,
    [Name] nvarchar(128) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Barangs] (
    [Id] int NOT NULL IDENTITY,
    [KodeBarang] nvarchar(50) NOT NULL,
    [NamaBarang] nvarchar(200) NOT NULL,
    [KategoriId] int NOT NULL,
    [Satuan] nvarchar(50) NOT NULL,
    [Stok] int NOT NULL,
    [StokMinimum] int NOT NULL,
    [Gambar] nvarchar(500) NULL,
    [Deskripsi] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Barangs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Barangs_Kategoris_KategoriId] FOREIGN KEY ([KategoriId]) REFERENCES [Kategoris] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [BarangKeluars] (
    [Id] int NOT NULL IDENTITY,
    [BarangId] int NOT NULL,
    [Jumlah] int NOT NULL,
    [TanggalKeluar] datetime2 NOT NULL,
    [Penerima] nvarchar(200) NOT NULL,
    [Alamat] nvarchar(500) NULL,
    [NoHpPenerima] nvarchar(20) NULL,
    [Keterangan] nvarchar(500) NULL,
    [Pic] nvarchar(100) NULL,
    [NoSuratJalan] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LokasiId] int NULL,
    CONSTRAINT [PK_BarangKeluars] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BarangKeluars_Barangs_BarangId] FOREIGN KEY ([BarangId]) REFERENCES [Barangs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BarangKeluars_Lokasis_LokasiId] FOREIGN KEY ([LokasiId]) REFERENCES [Lokasis] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [BarangLokasis] (
    [Id] int NOT NULL IDENTITY,
    [BarangId] int NOT NULL,
    [LokasiId] int NOT NULL,
    [Stok] int NOT NULL,
    CONSTRAINT [PK_BarangLokasis] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BarangLokasis_Barangs_BarangId] FOREIGN KEY ([BarangId]) REFERENCES [Barangs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BarangLokasis_Lokasis_LokasiId] FOREIGN KEY ([LokasiId]) REFERENCES [Lokasis] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [BarangMasuks] (
    [Id] int NOT NULL IDENTITY,
    [BarangId] int NOT NULL,
    [Jumlah] int NOT NULL,
    [TanggalMasuk] datetime2 NOT NULL,
    [HargaSatuan] decimal(18,2) NULL,
    [Keterangan] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LokasiId] int NULL,
    [SupplierId] int NULL,
    CONSTRAINT [PK_BarangMasuks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BarangMasuks_Barangs_BarangId] FOREIGN KEY ([BarangId]) REFERENCES [Barangs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BarangMasuks_Lokasis_LokasiId] FOREIGN KEY ([LokasiId]) REFERENCES [Lokasis] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BarangMasuks_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Peminjamans] (
    [Id] int NOT NULL IDENTITY,
    [BarangId] int NOT NULL,
    [Jumlah] int NOT NULL,
    [Peminjam] nvarchar(200) NOT NULL,
    [NipNik] nvarchar(50) NULL,
    [Departemen] nvarchar(100) NULL,
    [NoHp] nvarchar(20) NULL,
    [TanggalPinjam] datetime2 NOT NULL,
    [TanggalJatuhTempo] datetime2 NOT NULL,
    [TanggalKembali] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [KondisiKembali] nvarchar(50) NULL,
    [NoPeminjaman] nvarchar(100) NULL,
    [Keterangan] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Peminjamans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Peminjamans_Barangs_BarangId] FOREIGN KEY ([BarangId]) REFERENCES [Barangs] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [StokOpnameDetails] (
    [Id] int NOT NULL IDENTITY,
    [StokOpnameId] int NOT NULL,
    [BarangId] int NOT NULL,
    [StokSistem] int NOT NULL,
    [StokFisik] int NOT NULL,
    [Selisih] int NOT NULL,
    [Keterangan] nvarchar(500) NULL,
    CONSTRAINT [PK_StokOpnameDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StokOpnameDetails_Barangs_BarangId] FOREIGN KEY ([BarangId]) REFERENCES [Barangs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StokOpnameDetails_StokOpnames_StokOpnameId] FOREIGN KEY ([StokOpnameId]) REFERENCES [StokOpnames] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [TransferBarangs] (
    [Id] int NOT NULL IDENTITY,
    [BarangId] int NOT NULL,
    [DariLokasiId] int NOT NULL,
    [KeLokasiId] int NOT NULL,
    [Jumlah] int NOT NULL,
    [TanggalTransfer] datetime2 NOT NULL,
    [Keterangan] nvarchar(500) NULL,
    [NoTransfer] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TransferBarangs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TransferBarangs_Barangs_BarangId] FOREIGN KEY ([BarangId]) REFERENCES [Barangs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TransferBarangs_Lokasis_DariLokasiId] FOREIGN KEY ([DariLokasiId]) REFERENCES [Lokasis] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TransferBarangs_Lokasis_KeLokasiId] FOREIGN KEY ([KeLokasiId]) REFERENCES [Lokasis] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [BarangKembalis] (
    [Id] int NOT NULL IDENTITY,
    [BarangId] int NOT NULL,
    [BarangKeluarId] int NULL,
    [Jumlah] int NOT NULL,
    [TanggalKembali] datetime2 NOT NULL,
    [Kondisi] nvarchar(50) NOT NULL,
    [DikembalikanOleh] nvarchar(200) NOT NULL,
    [Keterangan] nvarchar(500) NULL,
    [TindakLanjut] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BarangKembalis] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BarangKembalis_BarangKeluars_BarangKeluarId] FOREIGN KEY ([BarangKeluarId]) REFERENCES [BarangKeluars] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BarangKembalis_Barangs_BarangId] FOREIGN KEY ([BarangId]) REFERENCES [Barangs] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [BarangSerials] (
    [Id] int NOT NULL IDENTITY,
    [BarangId] int NOT NULL,
    [SerialNumber] nvarchar(200) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [BarangMasukId] int NULL,
    [BarangKeluarId] int NULL,
    [Kondisi] nvarchar(100) NOT NULL,
    [BarangKembaliId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BarangSerials] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BarangSerials_BarangKeluars_BarangKeluarId] FOREIGN KEY ([BarangKeluarId]) REFERENCES [BarangKeluars] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BarangSerials_BarangKembalis_BarangKembaliId] FOREIGN KEY ([BarangKembaliId]) REFERENCES [BarangKembalis] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BarangSerials_BarangMasuks_BarangMasukId] FOREIGN KEY ([BarangMasukId]) REFERENCES [BarangMasuks] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BarangSerials_Barangs_BarangId] FOREIGN KEY ([BarangId]) REFERENCES [Barangs] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [TransferBarangSerials] (
    [Id] int NOT NULL IDENTITY,
    [TransferBarangId] int NOT NULL,
    [BarangSerialId] int NOT NULL,
    CONSTRAINT [PK_TransferBarangSerials] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TransferBarangSerials_BarangSerials_BarangSerialId] FOREIGN KEY ([BarangSerialId]) REFERENCES [BarangSerials] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TransferBarangSerials_TransferBarangs_TransferBarangId] FOREIGN KEY ([TransferBarangId]) REFERENCES [TransferBarangs] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE INDEX [IX_BarangKeluars_BarangId] ON [BarangKeluars] ([BarangId]);
GO

CREATE INDEX [IX_BarangKeluars_LokasiId] ON [BarangKeluars] ([LokasiId]);
GO

CREATE INDEX [IX_BarangKembalis_BarangId] ON [BarangKembalis] ([BarangId]);
GO

CREATE INDEX [IX_BarangKembalis_BarangKeluarId] ON [BarangKembalis] ([BarangKeluarId]);
GO

CREATE INDEX [IX_BarangLokasis_BarangId] ON [BarangLokasis] ([BarangId]);
GO

CREATE INDEX [IX_BarangLokasis_LokasiId] ON [BarangLokasis] ([LokasiId]);
GO

CREATE INDEX [IX_BarangMasuks_BarangId] ON [BarangMasuks] ([BarangId]);
GO

CREATE INDEX [IX_BarangMasuks_LokasiId] ON [BarangMasuks] ([LokasiId]);
GO

CREATE INDEX [IX_BarangMasuks_SupplierId] ON [BarangMasuks] ([SupplierId]);
GO

CREATE INDEX [IX_Barangs_KategoriId] ON [Barangs] ([KategoriId]);
GO

CREATE INDEX [IX_BarangSerials_BarangId] ON [BarangSerials] ([BarangId]);
GO

CREATE INDEX [IX_BarangSerials_BarangKeluarId] ON [BarangSerials] ([BarangKeluarId]);
GO

CREATE INDEX [IX_BarangSerials_BarangKembaliId] ON [BarangSerials] ([BarangKembaliId]);
GO

CREATE INDEX [IX_BarangSerials_BarangMasukId] ON [BarangSerials] ([BarangMasukId]);
GO

CREATE INDEX [IX_Peminjamans_BarangId] ON [Peminjamans] ([BarangId]);
GO

CREATE INDEX [IX_StokOpnameDetails_BarangId] ON [StokOpnameDetails] ([BarangId]);
GO

CREATE INDEX [IX_StokOpnameDetails_StokOpnameId] ON [StokOpnameDetails] ([StokOpnameId]);
GO

CREATE INDEX [IX_TransferBarangs_BarangId] ON [TransferBarangs] ([BarangId]);
GO

CREATE INDEX [IX_TransferBarangs_DariLokasiId] ON [TransferBarangs] ([DariLokasiId]);
GO

CREATE INDEX [IX_TransferBarangs_KeLokasiId] ON [TransferBarangs] ([KeLokasiId]);
GO

CREATE INDEX [IX_TransferBarangSerials_BarangSerialId] ON [TransferBarangSerials] ([BarangSerialId]);
GO

CREATE INDEX [IX_TransferBarangSerials_TransferBarangId] ON [TransferBarangSerials] ([TransferBarangId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260305035221_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Barangs] ADD [Merk] nvarchar(100) NULL;
GO

ALTER TABLE [Barangs] ADD [Type] nvarchar(100) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260305040935_AddMerkTypeToBarang', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [BarangLokasis] ADD [RakKompartemen] nvarchar(100) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260305051352_AddRakKompartemen', N'8.0.0');
GO

COMMIT;
GO

