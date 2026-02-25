# 📦 MyGudang — Sistem Manajemen Inventaris Modern

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-purple?logo=dotnet)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Express-red?logo=microsoftsqlserver)
![AdminLTE](https://img.shields.io/badge/AdminLTE-3.2-blue?logo=bootstrap)
![License](https://img.shields.io/badge/License-MIT-green)

**MyGudang** adalah aplikasi manajemen inventaris berbasis web yang dirancang untuk mengelola seluruh siklus hidup barang — dari penerimaan, penyimpanan, hingga pengeluaran — dengan pelacakan serial number dan multi-ruangan.

---

## ✨ Fitur Utama

### 📋 Manajemen Barang
- **Data Barang** — Katalog barang lengkap dengan kode, kategori, supplier, dan satuan
- **Auto-generate Kode Barang** — Kode otomatis berdasarkan prefix kategori (ELK-001, ATK-002, dst.)
- **Serial Number Tracking** — Lacak setiap unit barang secara individual
- **Multi-Ruangan** — Kelola stok di beberapa ruangan/lokasi sekaligus

### 📥 Barang Masuk
- Input barang masuk dengan jumlah, serial number, ruangan, dan supplier
- **Tambah Barang Baru** langsung dari form barang masuk (tanpa berpindah halaman)
- Serial number otomatis "-" untuk barang tanpa SN
- Stok otomatis bertambah di ruangan yang dipilih

### 📤 Barang Keluar
- Pilih barang & serial number via checkbox
- Barang tanpa SN tampil sebagai "Unit 1", "Unit 2", dst.
- Input penerima, alamat, dan no. HP
- Stok otomatis berkurang di ruangan asal
- **Surat Jalan & Surat Terima Barang** otomatis — siap cetak!

### 🔄 Transfer Antar Ruangan
- Pindahkan stok antar ruangan dengan mudah
- Riwayat transfer tercatat lengkap

### 🤝 Peminjaman
- Catat peminjaman barang dengan jatuh tempo
- Status: Dipinjam → Dikembalikan
- Surat peminjaman otomatis

### 🔙 Barang Kembali
- Catat pengembalian barang keluar
- Input kondisi & keterangan
- Stok otomatis bertambah kembali

### 📊 Dashboard & Laporan
- **Dashboard interaktif** dengan grafik Chart.js (konfigurasi warna & tipe chart)
- Kartu Stok per barang
- Stok Opname — bandingkan stok sistem vs fisik
- **Export/Import Excel** di setiap modul

### 📄 Surat Otomatis
- Surat Jalan dengan nomor otomatis
- Surat Terima Barang
- Surat Peminjaman & Pengembalian
- **Kop Surat editable** — logo, nama perusahaan, alamat
- **Nomor surat konfigurasi** — prefix & format bisa disesuaikan

### 🔐 Keamanan
- Login dengan ASP.NET Core Identity
- Role-based access: **SuperAdmin** & **User**
- Manajemen user (CRUD + reset password)

---

## 🛠️ Tech Stack

| Layer | Teknologi |
|-------|-----------|
| **Backend** | ASP.NET Core 8.0 MVC |
| **Database** | SQL Server Express + Entity Framework Core |
| **Frontend** | AdminLTE 3 + Bootstrap 4 |
| **Charts** | Chart.js |
| **Alerts** | SweetAlert2 |
| **Excel** | ClosedXML |
| **Auth** | ASP.NET Core Identity |

---

## 🚀 Instalasi

### Prasyarat
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)

### Langkah
```bash
# Clone repo
git clone https://github.com/danzone4u/MyGudang.git
cd MyGudang

# Update connection string di appsettings.json sesuai server SQL Anda

# Jalankan
dotnet run
```

Akses di **http://localhost:5156**

### Login Default
| Role | Username | Password |
|------|----------|----------|
| SuperAdmin | `admin` | `Admin123!` |

---

## 📁 Struktur Project

```
MyGudang/
├── Controllers/        # Logic bisnis per modul
├── Models/             # Entity model (EF Core)
├── Data/               # DbContext & konfigurasi
├── Views/              # Razor views per modul
│   ├── Dashboard/      # Halaman utama + chart
│   ├── Barang/         # CRUD barang
│   ├── BarangMasuk/    # Input barang masuk + modal barang baru
│   ├── BarangKeluar/   # Output barang + surat
│   ├── Peminjaman/     # Peminjaman + pengembalian
│   ├── Lokasi/         # Manajemen ruangan
│   └── Shared/         # Layout & partial views
├── wwwroot/            # Static files (CSS, JS, images)
└── Program.cs          # Entry point
```

---

## 📸 Alur Kerja

```
┌─────────────┐    ┌──────────────┐    ┌──────────────┐
│  Buat       │    │  Barang      │    │  Stok Auto   │
│  Kategori   │───▶│  Masuk       │───▶│  Bertambah   │
│  + Prefix   │    │  + Barang    │    │  di Ruangan  │
└─────────────┘    │    Baru      │    └──────┬───────┘
                   └──────────────┘           │
                                              ▼
┌─────────────┐    ┌──────────────┐    ┌──────────────┐
│  Cetak      │◀───│  Barang      │◀───│  Pilih       │
│  Surat      │    │  Keluar      │    │  Barang &    │
│  Jalan      │    │              │    │  Serial No   │
└─────────────┘    └──────────────┘    └──────────────┘
```

---

## 📝 License

MIT License — bebas digunakan untuk keperluan pribadi maupun komersial.

---

<p align="center">
  <b>Built with ❤️ using ASP.NET Core 8.0</b><br>
  <i>© 2026 MyGudang. All rights reserved.</i>
</p>
