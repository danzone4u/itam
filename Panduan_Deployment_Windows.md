# Panduan Deployment Proyek ITAM ke Windows Server (IIS & SQL Server)

Panduan ini berisi tahapan lengkap untuk men-*deploy* aplikasi **ITAM (IT Asset Management)** yang berbasis .NET 8 ke Windows Server menggunakan IIS (Internet Information Services) dan Microsoft SQL Server.

## 1. Prasyarat (Prerequisites)

Sebelum melakukan deployment, pastikan server (Windows Server) sudah terinstal:
1. **IIS (Internet Information Services)**: Dapat diaktifkan melalui *Server Manager* > *Add roles and features* > *Web Server (IIS)*.
2. **.NET 8.0 Hosting Bundle**: Wajib diinstal agar IIS dapat menjalankan aplikasi .NET Core 8. (Dapat diunduh dari situs resmi Microsoft .NET). *Catatan: Restart server atau IIS setelah instalasi bundle ini*.
3. **Microsoft SQL Server** (Misal: SQL Server Express atau edisi reguler) dan **SQL Server Management Studio (SSMS)**.

---

## 2. Persiapan Database (SQL Server)

Berdasarkan pengaturan terpusat (`appsettings.json`), aplikasi ITAM menggunakan database `itamDB` dan login akun `itam_user`.

1. Buka **SSMS** (SQL Server Management Studio) dan hubungkan ke server database Anda.
2. Buat database baru bernama `itamDB`:
   ```sql
   CREATE DATABASE itamDB;
   ```
3. Buka file `script.sql` yang ada pada root proyek ITAM anda, lalu *Execute* isinya di dalam database `itamDB`. Ini akan membuat struktur tabel (schema) yang dibutuhkan.
4. Buka file `CreateItamUser.sql` yang ada pada root proyek anda, periksa bagian password jika ingin menggunakan password yang lebih kuat, kemudian jalankan *Execute*.
   - Script ini otomatis membuat *Login* `itam_user` dengan password `PasswordAman123!`.
   - Memberikan akses *read*, *write*, dan akses untuk melakukan *Backup & Restore* ke database `itamDB` untuk fitur pencadangan aplikasi.

---

## 3. Konfigurasi `appsettings.json`

Pastikan file `appsettings.json` hasil publish (atau sebelum publish) memiliki connection string yang mengarah ke Server SQL Server Anda yang benar. 

Bentuk dasarnya:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=itamDB;User Id=itam_user;Password=PasswordAman123!;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```
*Jika menggunakan server berbeda, ubah `localhost\\SQLEXPRESS` menjadi nama server atau IP tujuan.*

---

## 4. Publish Proyek ITAM

Untuk mempersiapkan file yang akan diletakkan di Windows Server, Anda harus melakukan *publish*:

Jika melakukannya di Server/Mesin Pengembang:
1. Buka *Command Prompt* atau PowerShell, arahkan ke folder proyek `d:\Dev\itam`.
2. Jalankan perintah publish:
   ```cmd
   dotnet publish itam.csproj -c Release -o ./publish
   ```
3. Folder `publish` (di `d:\Dev\itam\publish`) inilah yang akan kita salin dan pakai di IIS. 
4. Pindahkan (Copy) folder `publish` ini ke dalam Server (misalnya di letakkan di `C:\inetpub\wwwroot\itam` atau path *drive* lain di server).

---

## 5. Konfigurasi IIS (Internet Information Services)

1. Buka **IIS Manager** di Windows Server.
2. **Buat Application Pool Baru**:
   - Di panel kiri, klik **Application Pools** > Pilih **Add Application Pool** (di sebelah kanan).
   - Name: `ItamAppPool`
   - .NET CLR version: **No Managed Code** (Sangat penting karena framework .NET 8 berjalan sebagai proses independen).
   - Managed pipeline mode: **Integrated**.
   - Klik OK.
3. **Tambahkan Website**:
   - Di panel kiri, klik kakan pada **Sites** > Pilih **Add Website**.
   - Site name: `ITAM Web` (atau sesuai keinginan).
   - Application pool: Pilih `ItamAppPool` (yang barusan Anda buat).
   - Physical path: Arahkan ke folder publish proyek Anda. (contoh: `C:\inetpub\wwwroot\itam`).
   - Binding: Tentukan Port yang akan digunakan (misal `80` atau `8080`) dan isi *Host name* apabila diperlukan.
   - Klik OK.

---

## 6. Pengaturan Hak Akses Folder (Permissions)

Aplikasi ITAM memiliki fungsi untuk mengunggah gambar/logo (seperti dalam kasus logo BAST) dan fungsionalitas lain sejenisnya sehingga IIS membutuhkan hak akses *Write* di folder tersebut.

1. Buka *File Explorer*, arahkan ke folder fisik website tersebut (misal `C:\inetpub\wwwroot\itam`).
2. Klik kanan pada folder tersebut > **Properties** > Tab **Security**.
3. Klik **Edit** > **Add...**.
4. Ketik `IIS_IUSRS` pada kotak, click **Check Names**, dan tekan OK.
5. Pada *Permissions for IIS_IUSRS*, pastikan untuk mencentang/memilih **Modify** dan **Write** di kolom *Allow*.
6. Klik **Apply** lalu **OK**.

---

## 7. Uji Coba

1. Buka Browser (Chrome, Edge, dll).
2. Masukkan alamat IP Server dan Port yang sudah di konfigurasikan (Contoh: `http://localhost/` atau `http://192.168.1.100:8080/`).
3. Aplikasi ITAM seharusnya sudah berjalan lancar dan siap digunakan. Jika ada fitur upload logo dari pembahasan sebelumnya, coba lakukan sebuah upload untuk memastikan *Permissions* telah dikonfigurasi dengan sempurna.

---

## 8. Mengelola Banyak Aplikasi di Satu Server (IIS)

Jika Server Windows Anda sudah digunakan untuk meng-host banyak aplikasi lain, Anda tidak bisa menggunakan Port 80 secara sembarangan karena akan *bentrok/conflict* dengan aplikasi existing. Berikut 3 cara mengatasinya:

### Opsi A: Menggunakan Port Berbeda (Paling Mudah)
Saat menambahkan Website di IIS (Langkah 5), ubah **Port** pada bagian **Binding** menjadi angka lain yang belum digunakan, misalnya `8080`, `8088`, atau `9000`.
- **Akses:** `http://ip-server:8080`
- *Catatan:* Pastikan Port tersebut sudah di-*allow* (diizinkan masuk) pada *Windows Firewall*.

### Opsi B: Menggunakan Host Name (Subdomain/Domain)
Jika memiliki DNS Server internal atau domain resmi, Anda dapat menggunakan port standar (80/443) bersamaan dengan aplikasi lain dengan mengatur **Host name**.
- Saat menabahkan Website (Langkah 5), isi bagian **Host name** dengan misalnya: `itam.nama-kantor.com`.
- IIS akan otomatis merutekan traffic ke aplikasi ITAM meskipun port 80 dipakaikan ke banyak aplikasi berbeda.
- **Akses:** `http://itam.nama-kantor.com`

### Opsi C: Menjadikan Sub-Aplikasi (Virtual Application)
Jika Anda ingin menggabungkan dengan Website utama yang sudah ada (misal `Default Web Site` atau `PortalUtama`):
1. Buka IIS, klik kanan pada Website yang sudah ada.
2. Pilih **Add Application...**
3. **Alias**: Isi dengan `itam`.
4. **Application pool**: Pilih `ItamAppPool` (Pastikan *No Managed Code*).
5. **Physical path**: Arahkan ke folder publish `d:\Dev\itam\publish` (atau `C:\inetpub\wwwroot\itam`).
6. **Akses:** `http://ip-server/itam`
