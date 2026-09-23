using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;
using itam.Services;
using ClosedXML.Excel;

namespace itam.Controllers
{
    [Authorize]
    public class PermintaanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITelegramService _telegram;
        private readonly IEmailService _email;

        public PermintaanController(ApplicationDbContext context, ITelegramService telegram, IEmailService email)
        {
            _context = context;
            _telegram = telegram;
            _email = email;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var query = _context.Permintaans
                .Include(p => p.Lokasi)
                .Include(p => p.Details)
                    .ThenInclude(d => d.Barang)
                .AsQueryable();

            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("AdminGudang") && !User.IsInRole("Supervisor"))
            {
                var username = User.Identity?.Name ?? "";
                query = query.Where(p => p.PemohonUser == username);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var data = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            ViewBag.CurrentStatus = status ?? "";
            ViewBag.PendingCount = await _context.Permintaans.CountAsync(p => p.Status == "Menunggu Approval");

            return View(data);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Barangs = await _context.Barangs
                .Where(b => b.Stok > 0)
                .OrderByDescending(b => b.IsOperasional)
                .ThenBy(b => b.NamaBarang)
                .ToListAsync();
            ViewBag.Lokasis = new SelectList(await _context.Lokasis.OrderBy(l => l.NamaLokasi).ToListAsync(), "Id", "NamaLokasi");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string jenisPermintaan,
            string penerima,
            string? nipNik,
            string? departemen,
            string? noHp,
            int? lokasiId,
            string keperluan,
            DateTime? tanggalDibutuhkan,
            int[] barangIds,
            int[] jumlahs,
            string[]? catatans)
        {
            if (barangIds == null || barangIds.Length == 0)
            {
                TempData["Error"] = "Pilih minimal 1 barang!";
                ViewBag.Barangs = await _context.Barangs.Where(b => b.Stok > 0).OrderByDescending(b => b.IsOperasional).ThenBy(b => b.NamaBarang).ToListAsync();
                ViewBag.Lokasis = new SelectList(await _context.Lokasis.OrderBy(l => l.NamaLokasi).ToListAsync(), "Id", "NamaLokasi", lokasiId);
                return View();
            }

            // Validasi stok
            for (int i = 0; i < barangIds.Length; i++)
            {
                var barang = await _context.Barangs.FindAsync(barangIds[i]);
                int jml = i < jumlahs.Length ? jumlahs[i] : 1;
                if (barang == null || barang.Stok < jml)
                {
                    TempData["Error"] = $"Stok barang '{barang?.NamaBarang ?? "Unknown"}' tidak mencukupi! (Tersedia: {barang?.Stok ?? 0}, Diminta: {jml})";
                    ViewBag.Barangs = await _context.Barangs.Where(b => b.Stok > 0).OrderByDescending(b => b.IsOperasional).ThenBy(b => b.NamaBarang).ToListAsync();
                    ViewBag.Lokasis = new SelectList(await _context.Lokasis.OrderBy(l => l.NamaLokasi).ToListAsync(), "Id", "NamaLokasi", lokasiId);
                    return View();
                }
            }

            var suratSetting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var baseCount = await _context.Permintaans.CountAsync() + 1;
            var noPermintaan = SuratSettingController.GenerateNomorSurat(suratSetting, baseCount, "REQ");

            var permintaan = new Permintaan
            {
                NoPermintaan = noPermintaan,
                JenisPermintaan = jenisPermintaan ?? "BarangKeluar",
                PemohonUser = User.Identity?.Name ?? "User",
                Penerima = penerima,
                NipNik = nipNik,
                Departemen = departemen,
                NoHp = noHp,
                LokasiId = lokasiId > 0 ? lokasiId : null,
                Keperluan = keperluan,
                TanggalPengajuan = DateTime.Now,
                TanggalDibutuhkan = tanggalDibutuhkan,
                Status = "Menunggu Approval",
                CreatedAt = DateTime.Now
            };

            _context.Permintaans.Add(permintaan);
            await _context.SaveChangesAsync();

            for (int i = 0; i < barangIds.Length; i++)
            {
                if (barangIds[i] <= 0) continue;
                var detail = new PermintaanDetail
                {
                    PermintaanId = permintaan.Id,
                    BarangId = barangIds[i],
                    Jumlah = i < jumlahs.Length ? jumlahs[i] : 1,
                    Catatan = (catatans != null && i < catatans.Length) ? catatans[i] : null
                };
                _context.PermintaanDetails.Add(detail);
            }
            await _context.SaveChangesAsync();

            // Telegram Notification to Admin
            try
            {
                var lokasiObj = lokasiId.HasValue && lokasiId.Value > 0 ? await _context.Lokasis.FindAsync(lokasiId.Value) : null;
                var jenisLabel = jenisPermintaan == "Peminjaman" ? "📌 *Peminjaman Barang*" : (jenisPermintaan == "BarangOperasional" ? "⚙️ *Barang Operasional*" : "📤 *Barang Keluar*");
                var msg = $"🔔 *Permintaan Barang Baru ({permintaan.NoPermintaan})*\n" +
                          $"Jenis: {jenisLabel}\n" +
                          $"Pemohon: *{permintaan.PemohonUser}*\n" +
                          $"Penerima: *{permintaan.Penerima}*\n" +
                          $"Unit Kerja: *{permintaan.Departemen ?? "-"}*\n" +
                          $"Lokasi: *{lokasiObj?.NamaLokasi ?? "Utama"}*\n" +
                          $"Keperluan: *{permintaan.Keperluan}*\n\n" +
                          $"*Item Diminta:*\n";

                var details = await _context.PermintaanDetails.Include(d => d.Barang).Where(d => d.PermintaanId == permintaan.Id).ToListAsync();
                foreach (var d in details)
                {
                    msg += $"- {d.Barang?.NamaBarang} ({d.Jumlah} {d.Barang?.Satuan})\n";
                }
                msg += "\n*Status: Menunggu Persetujuan Admin*";

                await _telegram.SendAsync(msg);
            }
            catch { }

            TempData["Success"] = $"Permintaan {noPermintaan} berhasil dibuat! Menunggu persetujuan Admin.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Detail(int id)
        {
            var permintaan = await _context.Permintaans
                .Include(p => p.Lokasi)
                .Include(p => p.BarangKeluar)
                .Include(p => p.Peminjaman)
                .Include(p => p.Details)
                    .ThenInclude(d => d.Barang)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permintaan == null)
            {
                TempData["Error"] = "Data permintaan tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("AdminGudang") && !User.IsInRole("Supervisor") && permintaan.PemohonUser != User.Identity?.Name)
            {
                return Forbid();
            }

            // Ambil daftar S/N tersedia untuk masing-masing item jika Admin / Supervisor
            var availableSerialsMap = new Dictionary<int, List<BarangSerial>>();
            if (User.IsInRole("SuperAdmin") || User.IsInRole("AdminGudang") || User.IsInRole("Supervisor"))
            {
                foreach (var detail in permintaan.Details)
                {
                    var serials = await _context.BarangSerials
                        .Where(s => s.BarangId == detail.BarangId && s.Status == "Tersedia" && s.SerialNumber != "-")
                        .OrderBy(s => s.SerialNumber)
                        .ToListAsync();
                    availableSerialsMap[detail.BarangId] = serials;
                }
            }
            ViewBag.AvailableSerialsMap = availableSerialsMap;

            return View(permintaan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var permintaan = await _context.Permintaans.FindAsync(id);
            if (permintaan == null) return NotFound();

            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("AdminGudang") && !User.IsInRole("Supervisor") && permintaan.PemohonUser != User.Identity?.Name)
            {
                return Forbid();
            }

            if (permintaan.Status != "Menunggu Approval")
            {
                TempData["Error"] = "Hanya permintaan berstatus 'Menunggu Approval' yang dapat dibatalkan.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            permintaan.Status = "Dibatalkan";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Permintaan berhasil dibatalkan.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang,Supervisor")]
        public async Task<IActionResult> Approve(int id, string? catatanAdmin)
        {
            var permintaan = await _context.Permintaans
                .Include(p => p.Details)
                    .ThenInclude(d => d.Barang)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permintaan == null) return NotFound();

            if (permintaan.Status != "Menunggu Approval")
            {
                TempData["Error"] = "Permintaan ini sudah diproses sebelumnya.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            // Check stock again before approving
            foreach (var detail in permintaan.Details)
            {
                var barang = await _context.Barangs.FindAsync(detail.BarangId);
                if (barang == null || barang.Stok < detail.Jumlah)
                {
                    TempData["Error"] = $"Gagal Approve! Stok '{barang?.NamaBarang ?? "Item"}' tidak cukup (Tersedia: {barang?.Stok ?? 0}, Diminta: {detail.Jumlah}).";
                    return RedirectToAction(nameof(Detail), new { id });
                }
            }

            var suratSetting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();

            if (permintaan.JenisPermintaan == "BarangKeluar")
            {
                // Process as BarangKeluar
                int bkCount = await _context.BarangKeluars.CountAsync() + 1;
                var sharedNoSuratJalan = SuratSettingController.GenerateNomorSurat(suratSetting, bkCount, "SJ");

                BarangKeluar? firstBk = null;

                foreach (var detail in permintaan.Details)
                {
                    var barang = await _context.Barangs.FindAsync(detail.BarangId);
                    if (barang == null) continue;

                    var bk = new BarangKeluar
                    {
                        BarangId = detail.BarangId,
                        Jumlah = detail.Jumlah,
                        TanggalKeluar = DateTime.Now,
                        Penerima = permintaan.Penerima,
                        NoHpPenerima = permintaan.NoHp,
                        Keterangan = $"[Request {permintaan.NoPermintaan}] {permintaan.Keperluan} - {detail.Catatan}".TrimEnd(' ', '-'),
                        Pic = permintaan.PemohonUser,
                        NoSuratJalan = sharedNoSuratJalan,
                        LokasiId = permintaan.LokasiId,
                        CreatedAt = DateTime.Now
                    };

                    _context.BarangKeluars.Add(bk);
                    barang.Stok = Math.Max(0, barang.Stok - detail.Jumlah);

                    // Potong stok lokasi jika ada
                    if (permintaan.LokasiId.HasValue)
                    {
                        var bl = await _context.BarangLokasis
                            .FirstOrDefaultAsync(x => x.BarangId == detail.BarangId && x.LokasiId == permintaan.LokasiId.Value);
                        if (bl != null)
                        {
                            bl.Stok = Math.Max(0, bl.Stok - detail.Jumlah);
                        }
                    }

                    await _context.SaveChangesAsync();

                    // Process available serials
                    var availableSerials = await _context.BarangSerials
                        .Where(s => s.BarangId == detail.BarangId && s.Status == "Tersedia")
                        .OrderBy(s => s.SerialNumber == "-" ? 0 : 1)
                        .ThenBy(s => s.Id)
                        .Take(detail.Jumlah)
                        .ToListAsync();

                    foreach (var s in availableSerials)
                    {
                        s.Status = "Keluar";
                        s.BarangKeluarId = bk.Id;
                    }
                    await _context.SaveChangesAsync();

                    if (firstBk == null) firstBk = bk;
                }

                permintaan.BarangKeluarId = firstBk?.Id;
                permintaan.Status = "Disetujui";
            }
            else if (permintaan.JenisPermintaan == "BarangOperasional")
            {
                // Process as BarangOperasional
                foreach (var detail in permintaan.Details)
                {
                    var barang = await _context.Barangs.FindAsync(detail.BarangId);
                    if (barang == null) continue;

                    barang.Stok = Math.Max(0, barang.Stok - detail.Jumlah);

                    if (permintaan.LokasiId.HasValue)
                    {
                        var bl = await _context.BarangLokasis
                            .FirstOrDefaultAsync(x => x.BarangId == detail.BarangId && x.LokasiId == permintaan.LokasiId.Value);
                        if (bl != null)
                        {
                            bl.Stok = Math.Max(0, bl.Stok - detail.Jumlah);
                        }
                    }

                    var availableSerials = await _context.BarangSerials
                        .Where(s => s.BarangId == detail.BarangId && s.Status == "Tersedia")
                        .OrderBy(s => s.SerialNumber == "-" ? 0 : 1)
                        .ThenBy(s => s.Id)
                        .Take(detail.Jumlah)
                        .ToListAsync();

                    foreach (var s in availableSerials)
                    {
                        s.Status = "Dipakai Operasional";
                    }
                }
                permintaan.Status = "Dipakai Operasional";
            }
            else
            {
                // Process as Peminjaman
                int pCount = await _context.Peminjamans.CountAsync() + 1;
                var sharedNoPeminjaman = SuratSettingController.GenerateNomorSurat(suratSetting, pCount, "SP");

                Peminjaman? firstPeminjaman = null;

                foreach (var detail in permintaan.Details)
                {
                    var barang = await _context.Barangs.FindAsync(detail.BarangId);
                    if (barang == null) continue;

                    var serialObj = await _context.BarangSerials
                        .Where(s => s.BarangId == detail.BarangId && s.Status == "Tersedia" && s.SerialNumber != "-")
                        .FirstOrDefaultAsync();

                    var p = new Peminjaman
                    {
                        BarangId = detail.BarangId,
                        BarangSerialId = serialObj?.Id,
                        Jumlah = detail.Jumlah,
                        Peminjam = permintaan.Penerima,
                        NipNik = permintaan.NipNik,
                        Departemen = permintaan.Departemen,
                        NoHp = permintaan.NoHp,
                        TanggalPinjam = DateTime.Now,
                        TanggalJatuhTempo = permintaan.TanggalDibutuhkan ?? DateTime.Now.AddDays(7),
                        Status = "Dipinjam",
                        NoPeminjaman = sharedNoPeminjaman,
                        Keterangan = $"[Request {permintaan.NoPermintaan}] {permintaan.Keperluan}".Trim(),
                        CreatedAt = DateTime.Now
                    };

                    _context.Peminjamans.Add(p);
                    barang.Stok = Math.Max(0, barang.Stok - detail.Jumlah);

                    if (serialObj != null)
                    {
                        serialObj.Status = "Dipinjam";
                    }

                    await _context.SaveChangesAsync();
                    if (firstPeminjaman == null) firstPeminjaman = p;
                }

                permintaan.PeminjamanId = firstPeminjaman?.Id;
                permintaan.Status = "Disetujui";
            }

            var approverName = User.FindFirst("NamaLengkap")?.Value ?? User.Identity?.Name ?? "Admin";
            permintaan.ApprovedBy = approverName;
            permintaan.ApprovedAt = DateTime.Now;
            permintaan.CatatanAdmin = catatanAdmin;

            await _context.SaveChangesAsync();

            // Send Telegram Notification
            try
            {
                var msg = $"✅ *Permintaan Barang DISETUJUI ({permintaan.NoPermintaan})*\n" +
                          $"Pemohon: *{permintaan.PemohonUser}*\n" +
                          $"Penerima: *{permintaan.Penerima}*\n" +
                          $"Disetujui Oleh: *{permintaan.ApprovedBy}*\n" +
                          $"Catatan Admin: *{permintaan.CatatanAdmin ?? "-"}*";
                await _telegram.SendAsync(msg);
            }
            catch { }

            TempData["Success"] = $"Permintaan {permintaan.NoPermintaan} berhasil DISETUJUI dan stok telah terpotong!";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang,Supervisor")]
        public async Task<IActionResult> Reject(int id, string catatanAdmin)
        {
            var permintaan = await _context.Permintaans.FindAsync(id);
            if (permintaan == null) return NotFound();

            if (permintaan.Status != "Menunggu Approval")
            {
                TempData["Error"] = "Permintaan ini sudah diproses sebelumnya.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var approverName = User.FindFirst("NamaLengkap")?.Value ?? User.Identity?.Name ?? "Admin";
            permintaan.Status = "Ditolak";
            permintaan.ApprovedBy = approverName;
            permintaan.ApprovedAt = DateTime.Now;
            permintaan.CatatanAdmin = string.IsNullOrWhiteSpace(catatanAdmin) ? "Ditolak oleh Admin" : catatanAdmin;

            await _context.SaveChangesAsync();

            try
            {
                var msg = $"❌ *Permintaan Barang DITOLAK ({permintaan.NoPermintaan})*\n" +
                          $"Pemohon: *{permintaan.PemohonUser}*\n" +
                          $"Ditolak Oleh: *{permintaan.ApprovedBy}*\n" +
                          $"Alasan Ditolak: *{permintaan.CatatanAdmin}*";
                await _telegram.SendAsync(msg);
            }
            catch { }

            TempData["Success"] = $"Permintaan {permintaan.NoPermintaan} DITOLAK.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        public async Task<IActionResult> SuratPermintaan(int id)
        {
            var permintaan = await _context.Permintaans
                .Include(p => p.Lokasi)
                .Include(p => p.Details)
                    .ThenInclude(d => d.Barang)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permintaan == null) return NotFound();

            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("AdminGudang") && !User.IsInRole("Supervisor") && permintaan.PemohonUser != User.Identity?.Name)
            {
                return Forbid();
            }

            ViewBag.Kop = await _context.KopSurats.FirstOrDefaultAsync() ?? new KopSurat();
            return View(permintaan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjukanPengembalian(int id, string? catatanPemohon)
        {
            var permintaan = await _context.Permintaans
                .Include(p => p.Details)
                    .ThenInclude(d => d.Barang)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permintaan == null) return NotFound();

            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("AdminGudang") && !User.IsInRole("Supervisor") && permintaan.PemohonUser != User.Identity?.Name)
            {
                return Forbid();
            }

            if (permintaan.Status != "Dipakai Operasional")
            {
                TempData["Error"] = "Hanya permintaan berstatus 'Dipakai Operasional' yang dapat diajukan pengembalian.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            permintaan.Status = "Menunggu Persetujuan Pengembalian";
            if (!string.IsNullOrWhiteSpace(catatanPemohon))
            {
                permintaan.CatatanAdmin = string.IsNullOrEmpty(permintaan.CatatanAdmin)
                    ? $"[Pemohon]: {catatanPemohon}"
                    : $"{permintaan.CatatanAdmin} | [Pemohon]: {catatanPemohon}";
            }

            await _context.SaveChangesAsync();

            try
            {
                var msg = $"🔔 *Pengajuan Pengembalian Barang Operasional ({permintaan.NoPermintaan})*\n" +
                          $"Pemohon: *{permintaan.PemohonUser}*\n" +
                          $"Penerima: *{permintaan.Penerima}*\n" +
                          $"Status: *Menunggu Persetujuan Admin*";
                await _telegram.SendAsync(msg);
            }
            catch { }

            TempData["Success"] = "Pengajuan pengembalian barang operasional berhasil dikirim! Menunggu persetujuan Admin.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang,Supervisor")]
        public async Task<IActionResult> SetujuiPengembalian(int id, string kondisi, string tindakLanjut, string? catatanAdmin)
        {
            var permintaan = await _context.Permintaans
                .Include(p => p.Details)
                    .ThenInclude(d => d.Barang)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (permintaan == null) return NotFound();

            if (permintaan.Status != "Menunggu Persetujuan Pengembalian" && permintaan.Status != "Dipakai Operasional")
            {
                TempData["Error"] = "Status permintaan tidak valid untuk disetujui pengembaliannya.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var selectedKondisi = string.IsNullOrEmpty(kondisi) ? "Baik" : kondisi;
            var selectedTindakLanjut = string.IsNullOrEmpty(tindakLanjut) ? "Dikembalikan ke Stok" : tindakLanjut;

            foreach (var detail in permintaan.Details)
            {
                var barangKembali = new BarangKembali
                {
                    BarangId = detail.BarangId,
                    Jumlah = detail.Jumlah,
                    TanggalKembali = DateTime.Now,
                    Kondisi = selectedKondisi,
                    DikembalikanOleh = permintaan.Penerima,
                    Keterangan = $"[Operasional {permintaan.NoPermintaan}] {catatanAdmin}".TrimEnd(),
                    TindakLanjut = selectedTindakLanjut,
                    CreatedAt = DateTime.Now
                };
                _context.BarangKembalis.Add(barangKembali);

                var barang = await _context.Barangs.FindAsync(detail.BarangId);
                if (barang != null && selectedTindakLanjut == "Dikembalikan ke Stok")
                {
                    barang.Stok += detail.Jumlah;

                    if (permintaan.LokasiId.HasValue)
                    {
                        var bl = await _context.BarangLokasis
                            .FirstOrDefaultAsync(x => x.BarangId == detail.BarangId && x.LokasiId == permintaan.LokasiId.Value);
                        if (bl != null)
                        {
                            bl.Stok += detail.Jumlah;
                        }
                    }

                    var serials = await _context.BarangSerials
                        .Where(s => s.BarangId == detail.BarangId && s.Status == "Dipakai Operasional")
                        .Take(detail.Jumlah)
                        .ToListAsync();
                    foreach (var s in serials)
                    {
                        s.Status = "Tersedia";
                        s.BarangKembaliId = barangKembali.Id;
                    }
                }
            }

            permintaan.Status = "Dikembalikan";
            if (!string.IsNullOrWhiteSpace(catatanAdmin))
            {
                permintaan.CatatanAdmin = string.IsNullOrEmpty(permintaan.CatatanAdmin)
                    ? $"[Admin Return]: {catatanAdmin}"
                    : $"{permintaan.CatatanAdmin} | [Admin Return]: {catatanAdmin}";
            }

            await _context.SaveChangesAsync();

            try
            {
                var msg = $"✅ *Pengembalian Barang Operasional DISETUJUI ({permintaan.NoPermintaan})*\n" +
                          $"Pemohon: *{permintaan.PemohonUser}*\n" +
                          $"Penerima: *{permintaan.Penerima}*\n" +
                          $"Kondisi: *{selectedKondisi}*\n" +
                          $"Tindak Lanjut: *{selectedTindakLanjut}*";
                await _telegram.SendAsync(msg);
            }
            catch { }

            TempData["Success"] = $"Pengembalian barang operasional {permintaan.NoPermintaan} berhasil DISETUJUI!";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(DateTime? from, DateTime? to, string? status, string? jenis)
        {
            var query = _context.Permintaans
                .Include(p => p.Lokasi)
                .Include(p => p.Details)
                    .ThenInclude(d => d.Barang)
                .AsQueryable();

            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("AdminGudang") && !User.IsInRole("Supervisor"))
            {
                var username = User.Identity?.Name ?? "";
                query = query.Where(p => p.PemohonUser == username);
            }

            if (from.HasValue) query = query.Where(p => p.TanggalPengajuan >= from.Value);
            if (to.HasValue) query = query.Where(p => p.TanggalPengajuan <= to.Value.AddDays(1));
            if (!string.IsNullOrEmpty(status)) query = query.Where(p => p.Status == status);
            if (!string.IsNullOrEmpty(jenis)) query = query.Where(p => p.JenisPermintaan == jenis);

            var data = await query.OrderByDescending(p => p.TanggalPengajuan).ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Permintaan Barang");

            ws.Cell(1, 1).Value = "Laporan Permintaan Barang & Peminjaman";
            ws.Range("A1:M1").Merge().Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;

            var row = 3;
            ws.Cell(row, 1).Value = "No";
            ws.Cell(row, 2).Value = "No. Permintaan";
            ws.Cell(row, 3).Value = "Jenis";
            ws.Cell(row, 4).Value = "Pemohon";
            ws.Cell(row, 5).Value = "Penerima";
            ws.Cell(row, 6).Value = "Nopeg/NIP";
            ws.Cell(row, 7).Value = "Departemen";
            ws.Cell(row, 8).Value = "Ruangan";
            ws.Cell(row, 9).Value = "Keperluan";
            ws.Cell(row, 10).Value = "Tgl Pengajuan";
            ws.Cell(row, 11).Value = "Status";
            ws.Cell(row, 12).Value = "Disetujui/Ditolak Oleh";
            ws.Cell(row, 13).Value = "Daftar Barang";
            ws.Range(row, 1, row, 13).Style.Font.Bold = true;
            ws.Range(row, 1, row, 13).Style.Fill.BackgroundColor = XLColor.FromHtml("#007bff");
            ws.Range(row, 1, row, 13).Style.Font.FontColor = XLColor.White;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.NoPermintaan;
                ws.Cell(row, 3).Value = item.JenisPermintaan == "Peminjaman" ? "Peminjaman" : "Barang Keluar";
                ws.Cell(row, 4).Value = item.PemohonUser;
                ws.Cell(row, 5).Value = item.Penerima;
                ws.Cell(row, 6).Value = item.NipNik ?? "-";
                ws.Cell(row, 7).Value = item.Departemen ?? "-";
                ws.Cell(row, 8).Value = item.Lokasi?.NamaLokasi ?? "-";
                ws.Cell(row, 9).Value = item.Keperluan;
                ws.Cell(row, 10).Value = item.TanggalPengajuan.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 11).Value = item.Status;
                ws.Cell(row, 12).Value = item.ApprovedBy ?? "-";

                var itemList = string.Join("; ", item.Details.Select(d => $"{d.Barang?.NamaBarang} ({d.Jumlah} {d.Barang?.Satuan})"));
                ws.Cell(row, 13).Value = itemList;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Laporan_Permintaan_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }
    }
}
