using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;
using itam.Services;

namespace itam.Controllers
{
    [Authorize(Roles = "SuperAdmin,AdminGudang")]
    public class PeminjamanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITelegramService _telegram;
        private readonly IEmailService _email;

        public PeminjamanController(ApplicationDbContext context, ITelegramService telegram, IEmailService email)
        {
            _context = context;
            _telegram = telegram;
            _email = email;
        }

        public async Task<IActionResult> Index()
        {
            // Auto-update status terlambat
            var overdue = await _context.Peminjamans
                .Where(p => p.Status == "Dipinjam" && p.TanggalJatuhTempo < DateTime.Now)
                .ToListAsync();
            foreach (var p in overdue) p.Status = "Terlambat";
            if (overdue.Any()) await _context.SaveChangesAsync();

            var data = await _context.Peminjamans
                .Include(p => p.Barang)
                .OrderByDescending(p => p.TanggalPinjam)
                .ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Barangs = await _context.Barangs.Where(b => b.Stok > 0).OrderBy(b => b.NamaBarang).ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSerials(int barangId)
        {
            var serials = await _context.BarangSerials
                .Where(s => s.BarangId == barangId && s.Status == "Tersedia" && s.SerialNumber != "-")
                .Select(s => new { id = s.Id, sn = s.SerialNumber })
                .ToListAsync();
            return Json(serials);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMultiple(int[] barangIds, int[] jumlahs, string[] keterangans, DateTime tanggalPinjam,
            DateTime tanggalJatuhTempo, string peminjam, string? nipNik, string? departemen, string? noHp, string? keteranganGlobal)
        {
            if (barangIds == null || barangIds.Length == 0)
            {
                TempData["Error"] = "Pilih minimal 1 barang!";
                return RedirectToAction(nameof(Create));
            }

            var suratSetting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var baseCount = await _context.Peminjamans.CountAsync();
            var noPeminjaman = GenerateNoPeminjaman(suratSetting, baseCount + 1);

            // Build a dictionary of row index -> list of selected SN IDs
            var snData = new Dictionary<int, List<int>>();
            var formKeys = Request.Form.Keys.Where(k => k.StartsWith("snRows["))
                .ToList();
            foreach (var key in formKeys)
            {
                // Extract numeric index from key (supports snRows[0] and snRows[0][])
                var match = System.Text.RegularExpressions.Regex.Match(key, @"snRows\[(\d+)\](?:\[\])?");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int idx))
                {
                    var vals = Request.Form[key]
                        .Where(v => !string.IsNullOrEmpty(v) && int.TryParse(v, out _))
                        .Select(v => int.Parse(v))
                        .ToList();
                    snData[idx] = vals;
                }
            }

            // Iterate over each barang row and associate its SN list (if any) by matching the row index directly
            int successCount = 0;
            for (int i = 0; i < barangIds.Length; i++)
            {
                var barang = await _context.Barangs.FindAsync(barangIds[i]);
                if (barang == null || barangIds[i] <= 0) continue;

                // Get SN list for this row index (i) if present; otherwise empty list
                var snList = snData.ContainsKey(i) ? snData[i] : new List<int>();

                int actualJumlah = snList.Count > 0 ? snList.Count : (i < jumlahs.Length ? jumlahs[i] : 1);
                if (actualJumlah > barang.Stok) actualJumlah = barang.Stok;
                if (actualJumlah <= 0) continue;

                var ket = (keterangans != null && i < keterangans.Length && !string.IsNullOrWhiteSpace(keterangans[i])
                    ? keterangans[i]
                    : keteranganGlobal);

                if (snList.Count > 0)
                {
                    foreach (var snId in snList)
                    {
                        var pinjam = new Peminjaman
                        {
                            BarangId = barangIds[i],
                            BarangSerialId = snId,
                            Jumlah = 1,
                            TanggalPinjam = tanggalPinjam,
                            TanggalJatuhTempo = tanggalJatuhTempo,
                            Peminjam = peminjam,
                            NipNik = nipNik,
                            Departemen = departemen,
                            NoHp = noHp,
                            Keterangan = ket,
                            NoPeminjaman = noPeminjaman,
                            Status = "Dipinjam",
                            CreatedAt = DateTime.Now
                        };
                        _context.Peminjamans.Add(pinjam);

                        var snObj = await _context.BarangSerials.FindAsync(snId);
                        if (snObj != null) snObj.Status = "Keluar";

                        barang.Stok = Math.Max(0, barang.Stok - 1);
                        successCount++;
                    }
                }
                else
                {
                    var pinjam = new Peminjaman
                    {
                        BarangId = barangIds[i],
                        Jumlah = actualJumlah,
                        TanggalPinjam = tanggalPinjam,
                        TanggalJatuhTempo = tanggalJatuhTempo,
                        Peminjam = peminjam,
                        NipNik = nipNik,
                        Departemen = departemen,
                        NoHp = noHp,
                        Keterangan = ket,
                        NoPeminjaman = noPeminjaman,
                        Status = "Dipinjam",
                        CreatedAt = DateTime.Now
                    };
                    _context.Peminjamans.Add(pinjam);
                    barang.Stok = Math.Max(0, barang.Stok - actualJumlah);
                    successCount++;
                }
            }

            await _context.SaveChangesAsync();

            // Kirim Notifikasi
            try
            {
                var addedItems = await _context.Peminjamans
                    .Include(p => p.Barang)
                    .Include(p => p.BarangSerial)
                    .Where(p => p.NoPeminjaman == noPeminjaman)
                    .ToListAsync();

                if (addedItems.Any())
                {
                    var firstItem = addedItems.First();
                    var itemDetails = string.Join("\n", addedItems.Select(item => 
                        $"- {item.Barang.NamaBarang} (Jml: {item.Jumlah}{(item.BarangSerial != null ? $", SN: {item.BarangSerial.SerialNumber}" : "")})"));

                    var msg = $"🔔 *Peminjaman Baru ({noPeminjaman})*\n" +
                              $"Peminjam: *{firstItem.Peminjam}*\n" +
                              $"Nopeg: *{firstItem.NipNik ?? "-"}*\n" +
                              $"Departemen: *{firstItem.Departemen ?? "-"}*\n" +
                              $"Tgl Pinjam: *{firstItem.TanggalPinjam:dd/MM/yyyy}*\n" +
                              $"Jatuh Tempo: *{firstItem.TanggalJatuhTempo:dd/MM/yyyy}*\n" +
                              $"Keterangan: *{keteranganGlobal ?? "-"}*\n\n" +
                              $"*Daftar Barang:*\n{itemDetails}";

                    if (await _context.TelegramBotSettings.AnyAsync(s => s.IsEnabled))
                    {
                        _ = Task.Run(() => _telegram.SendAsync(msg));
                    }
                    _ = Task.Run(() => _email.SendEmailAsync("Peminjaman Baru", msg, isPeminjaman: true));
                }
            }
            catch
            {
                // Silently ignore
            }

            TempData["Success"] = $"{successCount} item berhasil dipinjamkan!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> KembalikanTransaction(string noPeminjaman)
        {
            var items = await _context.Peminjamans.Include(p => p.Barang).Include(p => p.BarangSerial).Where(p => p.NoPeminjaman == noPeminjaman && p.Status != "Dikembalikan").ToListAsync();
            if (items == null || !items.Any()) return NotFound();
            return View("Kembalikan", items);
        }

        public async Task<IActionResult> DetailTransaction(string noPeminjaman)
        {
            var items = await _context.Peminjamans
                .Include(p => p.Barang)
                .Include(p => p.BarangSerial)
                .Where(p => p.NoPeminjaman == noPeminjaman)
                .ToListAsync();
            
            if (items == null || !items.Any()) return NotFound();
            
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KembalikanTransactionPost(string noPeminjaman, string kondisiKembali, string? keterangan)
        {
            var items = await _context.Peminjamans.Where(p => p.NoPeminjaman == noPeminjaman && p.Status != "Dikembalikan").ToListAsync();
            if (!items.Any()) return NotFound();

            foreach (var item in items)
            {
                item.TanggalKembali = DateTime.Now;
                item.Status = "Dikembalikan";
                item.KondisiKembali = kondisiKembali;
                if (!string.IsNullOrWhiteSpace(keterangan))
                    item.Keterangan = keterangan;

                var barang = await _context.Barangs.FindAsync(item.BarangId);
                if (barang != null) barang.Stok += item.Jumlah;

                if (item.BarangSerialId.HasValue)
                {
                    var snObj = await _context.BarangSerials.FindAsync(item.BarangSerialId.Value);
                    if (snObj != null) snObj.Status = "Tersedia";
                }
            }

            await _context.SaveChangesAsync();

            // Kirim Notifikasi
            try
            {
                var returnedItems = await _context.Peminjamans
                    .Include(p => p.Barang)
                    .Include(p => p.BarangSerial)
                    .Where(p => p.NoPeminjaman == noPeminjaman)
                    .ToListAsync();

                if (returnedItems.Any())
                {
                    var firstItem = returnedItems.First();
                    var itemDetails = string.Join("\n", returnedItems.Select(item => 
                        $"- {item.Barang.NamaBarang} (Jml: {item.Jumlah}{(item.BarangSerial != null ? $", SN: {item.BarangSerial.SerialNumber}" : "")})"));

                    var msg = $"✅ *Pengembalian Barang ({noPeminjaman})*\n" +
                              $"Peminjam: *{firstItem.Peminjam}*\n" +
                              $"Nopeg: *{firstItem.NipNik ?? "-"}*\n" +
                              $"Tgl Kembali: *{DateTime.Now:dd/MM/yyyy HH:mm}*\n" +
                              $"Kondisi: *{kondisiKembali}*\n" +
                              $"Keterangan: *{keterangan ?? "-"}*\n\n" +
                              $"*Daftar Barang:*\n{itemDetails}";

                    if (await _context.TelegramBotSettings.AnyAsync(s => s.IsEnabled))
                    {
                        _ = Task.Run(() => _telegram.SendAsync(msg));
                    }
                    _ = Task.Run(() => _email.SendEmailAsync("Pengembalian Barang", msg, isPeminjaman: true));
                }
            }
            catch
            {
                // Silently ignore
            }

            TempData["Success"] = "Seluruh barang dalam transaksi berhasil dikembalikan!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTransaction(string noPeminjaman)
        {
            var items = await _context.Peminjamans.Where(p => p.NoPeminjaman == noPeminjaman).ToListAsync();
            if (items.Any())
            {
                foreach (var item in items)
                {
                    if (item.Status != "Dikembalikan")
                    {
                        var barang = await _context.Barangs.FindAsync(item.BarangId);
                        if (barang != null) barang.Stok += item.Jumlah;

                        if (item.BarangSerialId.HasValue)
                        {
                            var snObj = await _context.BarangSerials.FindAsync(item.BarangSerialId.Value);
                            if (snObj != null) snObj.Status = "Tersedia";
                        }
                    }
                }
                _context.Peminjamans.RemoveRange(items);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Data transaksi peminjaman berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(string[] ids)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));
            var items = await _context.Peminjamans.Where(p => ids.Contains(p.NoPeminjaman)).ToListAsync();
            foreach (var item in items)
            {
                if (item.Status != "Dikembalikan")
                {
                    var barang = await _context.Barangs.FindAsync(item.BarangId);
                    if (barang != null) barang.Stok += item.Jumlah;

                    if (item.BarangSerialId.HasValue)
                    {
                        var snObj = await _context.BarangSerials.FindAsync(item.BarangSerialId.Value);
                        if (snObj != null) snObj.Status = "Tersedia";
                    }
                }
            }
            _context.Peminjamans.RemoveRange(items);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"{ids.Length} transaksi berhasil dihapus!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> SuratPeminjaman(int id)
        {
            var item = await _context.Peminjamans.Include(p => p.Barang).Include(p => p.BarangSerial).FirstOrDefaultAsync(p => p.Id == id);
            if (item == null) return NotFound();
            ViewBag.Kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync() ?? new KopSurat();
            
            var suratSetting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var count = await _context.Peminjamans.Where(b => b.Id <= id).CountAsync(); // Note: This might not be perfectly accurate if id is not the first one, but it works as is
            ViewBag.NoSuratPeminjaman = item.NoPeminjaman ?? SuratSettingController.GenerateNomorSurat(suratSetting, count, "SP");
            
            var allItems = await _context.Peminjamans
                .Include(p => p.Barang)
                .Include(p => p.BarangSerial)
                .Where(p => p.NoPeminjaman == item.NoPeminjaman)
                .ToListAsync();

            ViewBag.AllItems = allItems;

            return View(item);
        }

        private string GenerateNoPeminjaman(SuratSetting? setting, int counter)
        {
            return SuratSettingController.GenerateNomorSurat(setting, counter, "SP");
        }
    }
}
