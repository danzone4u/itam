# 📦 ITAM (IT Asset Management) - MyGudang

**ITAM (MyGudang)** adalah Sistem Informasi Manajemen Inventaris & Aset TI berbasis web yang dirancang khusus untuk mencatat, melacak, dan mengelola seluruh aktivitas pergudangan secara digital dan terpusat. Aplikasi ini dikembangkan untuk memastikan akurasi data stok barang, meminimalisir kehilangan, serta mempermudah pembuatan laporan dan dokumen resmi dengan standar profesional.

Aplikasi ini dikembangkan oleh **IT Region Jatimbalinus - PT Pertamina Patra Niaga**.

---

## 🌟 Fitur Unggulan (Core Features)

1.  **Dashboard & Statistik Real-time**
    Visualisasi data stok, mutasi bulanan, dan peringatan stok minimum dalam grafik interaktif yang dapat dikustomisasi.

2.  **Sistem Penomoran Dokumen Dinamis (Template-Based)**
    Fleksibilitas penuh dalam menentukan format Nomor Surat Jalan dan BAST menggunakan tag otomatis seperti `{PREFIX}`, `{NOMOR}`, `{TAHUN}`, `{BULAN_ROMAWI}`, dll. Mendukung reset counter secara **Bulanan** maupun **Tahunan**.

3.  **Import Data Cerdas (Smart Import)**
    Fasilitas import data via Excel yang andal. Sistem secara otomatis mendeteksi dan melewati (*skip*) Serial Number yang sudah ada (duplikat) tanpa menggagalkan seluruh proses import, lengkap dengan laporan warning detail.

4.  **Manajemen Master Data & Lokasi**
    Pengelolaan kategori, supplier, dan ruangan penyimpanan yang terintegrasi. Dilengkapi dengan proteksi penghapusan data (data safety) jika masih memiliki keterkaitan dengan inventaris aktif.

5.  **Pelacakan Serial Number (S/N)**
    Manajemen aset presisi tinggi dengan pelacakan nomor seri unik pada setiap tahap: Barang Masuk, Keluar, Kembali, hingga Transfer antar ruangan.

6.  **Siklus Inventaris Lengkap**
    *   **Barang Masuk & Keluar**: Cetak Surat Jalan & BAST secara instan.
    *   **Peminjaman**: Reservasi aset sementara dengan notifikasi jatuh tempo.
    *   **Transfer Ruangan**: Mutasi internal antar lokasi tanpa mengubah total stok global.
    *   **Barang Kembali**: Penanganan barang retur dengan opsi tindak lanjut (Restock atau Disposal).

7.  **Audit & Keamanan**
    *   **Stok Opname**: Modul audit fisik untuk sinkronisasi stok sistem vs lapangan.
    *   **Log Aktivitas**: Rekam jejak audit (audit trail) untuk setiap tindakan user.
    *   **Auto-Backup**: Pencadangan database otomatis terjadwal ke directory lokal.

---

## 🛠️ Teknologi yang Digunakan (Tech Stack)

| Kategori | Teknologi / Library |
|----------|---------------------|
| **Backend** | ASP.NET Core MVC 8.0 (LTS) |
| **Database** | MS SQL Server + EF Core (Code First) |
| **Authentication** | ASP.NET Core Identity (Identity Framework) |
| **UI Framework** | AdminLTE v3.2 + Bootstrap 4 |
| **Interactivity** | DataTables.js, Select2, SweetAlert2 |
| **Report Engine**| ClosedXML & EPPlus (Excel Generation) |
| **Deployment** | IIS 10+ Support (Sub-folder ready) |

---

## 🚀 Panduan Deployment (IIS)

Aplikasi ini telah dioptimalkan untuk berjalan sebagai **Sub-Application** di IIS (misal: `domain.com/itam/`).

### 1. Konfigurasi `appsettings.json`
Pastikan Anda menambahkan kunci `PathBase` agar routing dan cookie berjalan dengan benar:
```json
{
  "PathBase": "/itam",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=itamDB;User Id=...;Password=..."
  }
}
```

### 2. Folder Izin (Permissions)
Berikan izin **Write / Full Control** kepada user IIS (App Pool) untuk folder berikut:
*   `wwwroot/uploads`: Tempat penyimpanan gambar logo, favicon, dan foto barang.
*   `DataProtectionKeys`: Folder penyimpanan kunci enkripsi sesi login.

### 3. Database Migration
Aplikasi akan otomatis menjalankan migrasi saat pertama kali dijalankan. Pastikan akun SQL Server memiliki izin untuk memodifikasi schema (`db_owner`).

---

## 📊 Entity Relationship Diagram (ERD)

Diagram di bawah ini menunjukkan struktur relasi database lengkap dari sistem ITAM:

```mermaid
erDiagram
    %% Master Data Relationships
    KATEGORI ||--o{ BARANG : "has"
    SUPPLIER ||--o{ BARANG_MASUK : "source for"
    LOKASI ||--o{ BARANG_LOKASI : "manages"
    LOKASI ||--o{ BARANG_MASUK : "destination"
    LOKASI ||--o{ BARANG_KELUAR : "source"
    LOKASI ||--o{ TRANSFER_BARANG : "from/to"
    
    %% Transaction Relationships
    BARANG ||--o{ BARANG_MASUK : "received"
    BARANG ||--o{ BARANG_KELUAR : "distributed"
    BARANG ||--o{ BARANG_KEMBALI : "returned"
    BARANG ||--o{ PEMINJAMAN : "borrowed"
    BARANG ||--o{ TRANSFER_BARANG : "moved"
    BARANG ||--o{ BARANG_LOKASI : "inventory"
    BARANG ||--o{ STOK_OPNAME_DETAIL : "audited"
    BARANG_KELUAR ||--o{ BARANG_KEMBALI : "referred by"
    
    %% Serial Number Tracking
    BARANG ||--o{ BARANG_SERIAL : "has"
    BARANG_MASUK ||--o{ BARANG_SERIAL : "registers"
    BARANG_KELUAR ||--o{ BARANG_SERIAL : "targets"
    BARANG_KEMBALI ||--o{ BARANG_SERIAL : "reclaims"
    TRANSFER_BARANG ||--o{ TRANSFER_BARANG_SERIAL : "triggers"
    BARANG_SERIAL ||--o{ TRANSFER_BARANG_SERIAL : "identified by"
    
    %% Audit Modules
    STOK_OPNAME ||--o{ STOK_OPNAME_DETAIL : "records"
    
    %% System Log
    IDENTITY_USER ||--o{ ACTIVITY_LOG : "performs"

    KOP_SURAT {
        int Id PK
        string NamaPerusahaan
        string SubJudul
        string LogoPath
    }

    APP_SETTING {
        int Id PK
        string AppName
        string LogoPath
        string FaviconPath
    }

    BACKUP_SETTING {
        int Id PK
        string BackupPath
        bool IsAutoBackupEnabled
        time BackupTime
    }

    ACTIVITY_LOG {
        int Id PK
        string User
        string Action
        string Module
        datetime Timestamp
    }

    KATEGORI {
        int Id PK
        string NamaKategori
        string KodePrefix
    }

    SUPPLIER {
        int Id PK
        string NamaSupplier
        string Kontak
        string Alamat
    }

    BARANG {
        int Id PK
        string KodeBarang
        string NamaBarang
        int KategoriId FK
        string Satuan
        int Stok
        int StokMinimum
        string Gambar
        datetime CreatedAt
        datetime UpdatedAt
    }

    LOKASI {
        int Id PK
        string Kode
        string NamaLokasi
    }

    BARANG_LOKASI {
        int Id PK
        int BarangId FK
        int LokasiId FK
        int Stok
    }

    BARANG_MASUK {
        int Id PK
        int BarangId FK
        int SupplierId FK
        int LokasiId FK
        int Jumlah
        datetime TanggalMasuk
        decimal HargaSatuan
        string NoSuratJalan
    }

    BARANG_KELUAR {
        int Id PK
        int BarangId FK
        int LokasiId FK
        int Jumlah
        datetime TanggalKeluar
        string Penerima
        string NoSuratJalan
    }

    BARANG_SERIAL {
        int Id PK
        int BarangId FK
        string SerialNumber
        string Status
        int BarangMasukId FK
        int BarangKeluarId FK
    }

    PEMINJAMAN {
        int Id PK
        int BarangId FK
        string NoPeminjaman
        string Peminjam
        datetime TanggalPinjam
        datetime TanggalJatuhTempo
        string Status
    }

    BARANG_KEMBALI {
        int Id PK
        int BarangId FK
        int BarangKeluarId FK
        int Jumlah
        datetime TanggalKembali
        string TindakLanjut
    }

    TRANSFER_BARANG {
        int Id PK
        int BarangId FK
        int DariLokasiId FK
        int KeLokasiId FK
        int Jumlah
        datetime TanggalTransfer
    }

    STOK_OPNAME {
        int Id PK
        datetime TanggalOpname
        string Status
    }

    STOK_OPNAME_DETAIL {
        int Id PK
        int StokOpnameId FK
        int BarangId FK
        int StokSistem
        int StokFisik
        int Selisih
    }

    ARSIP {
        int Id PK
        string NamaDokumen
        string NomorDokumen
        string FilePath
    }

    SURAT_SETTING {
        int Id PK
        string JenisSurat
        string TemplateSurat
        int CounterSekarang
        bool ResetTahunan
    }
    
    APP_SETTING {
        int Id
        string AppName
        string LogoPath
        string FaviconPath
    }
```

---

## 🔒 Tingkat Akses (Role Management)

*   **SuperAdmin**: Akses penuh ke seluruh modul, manajemen user, log aktivitas, dan pengaturan sistem (Backup/Kop Surat).
*   **Admin**: Akses operasional harian (Barang Masuk/Keluar, Stok, Laporan), namun tidak dapat mengelola user atau menghapus log sistem.

---
> _Developed with ❤️ by IT Region Jatimbalinus - PT Pertamina Patra Niaga_
