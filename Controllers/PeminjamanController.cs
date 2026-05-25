using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;

namespace itam.Controllers
{
    [Authorize(Roles = "SuperAdmin,AdminGudang")]
    public class PeminjamanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PeminjamanController(ApplicationDbContext context)
        {
            _context = context;
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

            var snData = new Dictionary<int, List<int>>();
            var formKeys = Request.Form.Keys.Where(k => k.StartsWith("snRows[")).ToList();
            foreach (var key in formKeys)
            {
                var indexStr = key.Replace("snRows[", "").Replace("]", "");
                if (int.TryParse(indexStr, out int idx))
                {
                    var vals = Request.Form[key].Where(v => !string.IsNullOrEmpty(v) && int.TryParse(v, out _)).Select(v => int.Parse(v!)).ToList();
                    snData[idx] = vals;
                }
            }

            var orderedSnKeys = snData.Keys.OrderBy(k => k).ToList();
            int successCount = 0;

            for (int i = 0; i < barangIds.Length; i++)
            {
                var barang = await _context.Barangs.FindAsync(barangIds[i]);
                if (barang == null || barangIds[i] <= 0) continue;
                
                var snList = new List<int>();
                if (orderedSnKeys.Count > i)
                {
                    int keyIndex = orderedSnKeys[i];
                    snList = snData[keyIndex];
                }

                int actualJumlah = snList.Count > 0 ? snList.Count : (i < jumlahs.Length ? jumlahs[i] : 1);
                if (actualJumlah > barang.Stok) actualJumlah = barang.Stok;
                if (actualJumlah <= 0) continue;

                var ket = (keterangans != null && i < keterangans.Length && !string.IsNullOrWhiteSpace(keterangans[i]))
                    ? keterangans[i] : keteranganGlobal;

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

                        barang.Stok -= 1;
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
                    barang.Stok -= actualJumlah;
                    successCount++;
                }
            }

            await _context.SaveChangesAsync();
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
