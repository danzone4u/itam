# Entity Relationship Diagram — ITAM

Diagram berikut menggambarkan seluruh entitas dan relasi dalam sistem **IT Asset Management (ITAM)** berdasarkan model C# yang ada di folder `Models/`.

```mermaid
erDiagram

    Kategori {
        int Id PK
        string NamaKategori
        string KodePrefix
        string Deskripsi
        datetime CreatedAt
    }

    Barang {
        int Id PK
        string KodeBarang
        string NamaBarang
        int KategoriId FK
        string Satuan
        int Stok
        int StokMinimum
        string Gambar
        string Deskripsi
        datetime CreatedAt
        datetime UpdatedAt
    }

    Lokasi {
        int Id PK
        string Kode
        string NamaLokasi
        string Alamat
        string PenanggungJawab
        string NoTelp
        datetime CreatedAt
    }

    Supplier {
        int Id PK
        string NamaSupplier
        string Alamat
        string Telepon
        string Email
        datetime CreatedAt
    }

    BarangLokasi {
        int Id PK
        int BarangId FK
        int LokasiId FK
        int Stok
        string RakKompartemen
    }

    BarangMasuk {
        int Id PK
        int BarangId FK
        int Jumlah
        datetime TanggalMasuk
        decimal HargaSatuan
        string Keterangan
        int LokasiId FK
        int SupplierId FK
        string StatusKepemilikan
        datetime TanggalJatuhTempoSewa
        datetime CreatedAt
    }

    BarangKeluar {
        int Id PK
        int BarangId FK
        int Jumlah
        datetime TanggalKeluar
        string Penerima
        string Alamat
        string NoHpPenerima
        string Keterangan
        string Pic
        string NoSuratJalan
        int LokasiId FK
        datetime CreatedAt
    }

    BarangKembali {
        int Id PK
        int BarangId FK
        int BarangKeluarId FK
        int Jumlah
        datetime TanggalKembali
        string Kondisi
        string DikembalikanOleh
        string Keterangan
        string TindakLanjut
        datetime CreatedAt
    }

    BarangSerial {
        int Id PK
        int BarangId FK
        string SerialNumber
        string Status
        int BarangMasukId FK
        int BarangKeluarId FK
        int BarangKembaliId FK
        string Kondisi
        datetime CreatedAt
    }

    Peminjaman {
        int Id PK
        int BarangId FK
        int BarangSerialId FK
        int Jumlah
        string Peminjam
        string NipNik
        string Departemen
        string NoHp
        datetime TanggalPinjam
        datetime TanggalJatuhTempo
        datetime TanggalKembali
        string Status
        string KondisiKembali
        string NoPeminjaman
        string Keterangan
        datetime CreatedAt
    }

    TransferBarang {
        int Id PK
        int BarangId FK
        int DariLokasiId FK
        int KeLokasiId FK
        int Jumlah
        datetime TanggalTransfer
        string Keterangan
        string NoTransfer
        datetime CreatedAt
    }

    TransferBarangSerial {
        int Id PK
        int TransferBarangId FK
        int BarangSerialId FK
    }

    StokOpname {
        int Id PK
        datetime TanggalOpname
        string Keterangan
        string Status
        datetime CreatedAt
    }

    StokOpnameDetail {
        int Id PK
        int StokOpnameId FK
        int BarangId FK
        int StokSistem
        int StokFisik
        int Selisih
        string Keterangan
    }

    Arsip {
        int Id PK
        string NamaDokumen
        string NomorDokumen
        string JenisDokumen
        string FilePath
        string NamaFile
        string Keterangan
        datetime CreatedAt
    }

    ActivityLog {
        int Id PK
        string UserName
        string Action
        string Module
        string Detail
        datetime CreatedAt
        string IpAddress
    }

    KopSurat {
        int Id PK
        string NamaPerusahaan
        string SubJudul
        string Alamat
        string Telepon
        string Email
        string Website
        string NamaPengirim
        string JabatanPengirim
        bool TampilkanLogo
        string LogoPath
    }

    %% === RELASI ===

    Kategori ||--o{ Barang : "memiliki"
    Barang ||--o{ BarangMasuk : "barang masuk"
    Barang ||--o{ BarangKeluar : "barang keluar"
    Barang ||--o{ BarangKembali : "barang kembali"
    Barang ||--o{ BarangSerial : "serial numbers"
    Barang ||--o{ BarangLokasi : "stok per lokasi"
    Barang ||--o{ Peminjaman : "dipinjam"
    Barang ||--o{ TransferBarang : "ditransfer"
    Barang ||--o{ StokOpnameDetail : "diopname"

    Lokasi ||--o{ BarangLokasi : "menyimpan"
    Lokasi ||--o{ BarangMasuk : "tujuan masuk"
    Lokasi ||--o{ BarangKeluar : "asal keluar"
    Lokasi ||--o{ TransferBarang : "dari lokasi"
    Lokasi ||--o{ TransferBarang : "ke lokasi"

    Supplier ||--o{ BarangMasuk : "memasok"

    BarangKeluar ||--o{ BarangKembali : "referensi kembali"
    BarangKeluar ||--o{ BarangSerial : "serial keluar"

    BarangMasuk ||--o{ BarangSerial : "serial masuk"
    BarangKembali ||--o{ BarangSerial : "serial kembali"

    BarangSerial ||--o{ Peminjaman : "serial dipinjam"
    BarangSerial ||--o{ TransferBarangSerial : "serial ditransfer"

    TransferBarang ||--o{ TransferBarangSerial : "detail serial"

    StokOpname ||--o{ StokOpnameDetail : "detail opname"
```

---

## Ringkasan Relasi Utama

| Relasi | Tipe | Keterangan |
|--------|------|------------|
| **Kategori → Barang** | 1 : N | Satu kategori memiliki banyak barang |
| **Barang → BarangMasuk** | 1 : N | Barang bisa masuk berkali-kali |
| **Barang → BarangKeluar** | 1 : N | Barang bisa keluar berkali-kali |
| **Barang → BarangKembali** | 1 : N | Barang bisa dikembalikan berkali-kali |
| **Barang → BarangSerial** | 1 : N | Barang bisa punya banyak serial number |
| **Barang ↔ Lokasi** (via BarangLokasi) | N : M | Stok per barang per lokasi (junction table) |
| **Supplier → BarangMasuk** | 1 : N | Satu supplier bisa memasok banyak transaksi masuk |
| **Lokasi → BarangMasuk / BarangKeluar** | 1 : N | Lokasi tujuan/asal transaksi |
| **BarangKeluar → BarangKembali** | 1 : N | Pengembalian mereferensi barang keluar |
| **BarangSerial → BarangMasuk / BarangKeluar / BarangKembali** | N : 1 | Serial terkait ke transaksi masuk, keluar, atau kembali |
| **Barang → Peminjaman** | 1 : N | Barang bisa dipinjam berkali-kali |
| **BarangSerial → Peminjaman** | 1 : N | Peminjaman bisa merujuk serial tertentu |
| **TransferBarang → Lokasi** (2 FK) | N : 1 | Transfer dari satu lokasi ke lokasi lain |
| **TransferBarang ↔ BarangSerial** (via TransferBarangSerial) | N : M | Serial yang ikut ditransfer |
| **StokOpname → StokOpnameDetail → Barang** | 1 : N : 1 | Detail opname per barang |

> **Arsip**, **ActivityLog**, dan **KopSurat** adalah entitas standalone tanpa relasi ke entitas lain.
