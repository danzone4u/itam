using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;

namespace MyGudang.Controllers
{
    [Authorize]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMultiple(int[] barangIds, int[] jumlahs, DateTime tanggalPinjam,
            DateTime tanggalJatuhTempo, string peminjam, string? nipNik, string? departemen, string? noHp, string? keteranganGlobal)
        {
            if (barangIds == null || barangIds.Length == 0)
            {
                TempData["Error"] = "Pilih minimal 1 barang!";
                return RedirectToAction(nameof(Create));
            }

            var suratSetting = await _context.SuratSettings.FirstOrDefaultAsync();
            var baseCount = await _context.Peminjamans.CountAsync();
            var noPeminjaman = GenerateNoPeminjaman(suratSetting, baseCount + 1);

            for (int i = 0; i < barangIds.Length; i++)
            {
                var barang = await _context.Barangs.FindAsync(barangIds[i]);
                if (barang == null) continue;
                var jumlah = (i < jumlahs.Length) ? jumlahs[i] : 1;
                if (jumlah > barang.Stok) jumlah = barang.Stok;

                var pinjam = new Peminjaman
                {
                    BarangId = barangIds[i],
                    Jumlah = jumlah,
                    TanggalPinjam = tanggalPinjam,
                    TanggalJatuhTempo = tanggalJatuhTempo,
                    Peminjam = peminjam,
                    NipNik = nipNik,
                    Departemen = departemen,
                    NoHp = noHp,
                    Keterangan = keteranganGlobal,
                    NoPeminjaman = noPeminjaman,
                    Status = "Dipinjam",
                    CreatedAt = DateTime.Now
                };

                barang.Stok -= jumlah;
                _context.Peminjamans.Add(pinjam);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{barangIds.Length} barang berhasil dipinjamkan!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Kembalikan(int id)
        {
            var item = await _context.Peminjamans.Include(p => p.Barang).FirstOrDefaultAsync(p => p.Id == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kembalikan(int id, string kondisiKembali, string? keterangan)
        {
            var item = await _context.Peminjamans.FindAsync(id);
            if (item == null) return NotFound();

            item.TanggalKembali = DateTime.Now;
            item.Status = "Dikembalikan";
            item.KondisiKembali = kondisiKembali;
            if (!string.IsNullOrWhiteSpace(keterangan))
                item.Keterangan = keterangan;

            // Kembalikan stok
            var barang = await _context.Barangs.FindAsync(item.BarangId);
            if (barang != null) barang.Stok += item.Jumlah;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Barang berhasil dikembalikan!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Peminjamans.FindAsync(id);
            if (item != null)
            {
                // Jika masih dipinjam, kembalikan stok
                if (item.Status != "Dikembalikan")
                {
                    var barang = await _context.Barangs.FindAsync(item.BarangId);
                    if (barang != null) barang.Stok += item.Jumlah;
                }
                _context.Peminjamans.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Data peminjaman berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(int[] ids)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));
            var items = await _context.Peminjamans.Where(p => ids.Contains(p.Id)).ToListAsync();
            foreach (var item in items)
            {
                if (item.Status != "Dikembalikan")
                {
                    var barang = await _context.Barangs.FindAsync(item.BarangId);
                    if (barang != null) barang.Stok += item.Jumlah;
                }
            }
            _context.Peminjamans.RemoveRange(items);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"{items.Count} data peminjaman berhasil dihapus!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> SuratPeminjaman(int id)
        {
            var item = await _context.Peminjamans.Include(p => p.Barang).FirstOrDefaultAsync(p => p.Id == id);
            if (item == null) return NotFound();
            ViewBag.Kop = await _context.KopSurats.FirstOrDefaultAsync() ?? new KopSurat();
            return View(item);
        }

        private string GenerateNoPeminjaman(SuratSetting? setting, int counter)
        {
            if (setting == null) setting = new SuratSetting();
            var sep = setting.Separator ?? "-";
            var tanggal = DateTime.Now.ToString(setting.FormatTanggal ?? "yyyyMMdd");
            var nomor = counter.ToString($"D{setting.PanjangNomorUrut}");
            var prefix = "PJM";
            return string.IsNullOrWhiteSpace(setting.Suffix)
                ? $"{prefix}{sep}{tanggal}{sep}{nomor}"
                : $"{prefix}{sep}{setting.Suffix}{sep}{tanggal}{sep}{nomor}";
        }
    }
}
