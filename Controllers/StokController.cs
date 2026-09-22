using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;

namespace itam.Controllers
{
    [Authorize]
    public class StokController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Services.IStokSyncService _stokSync;

        public StokController(ApplicationDbContext context, Services.IStokSyncService stokSync)
        {
            _context = context;
            _stokSync = stokSync;
        }

        public async Task<IActionResult> Index(string? search, int? kategoriId)
        {
            // Auto-heal negative stok if any exists in DB
            var negBarangs = await _context.Barangs.Where(b => b.Stok < 0).ToListAsync();
            if (negBarangs.Any())
            {
                foreach (var b in negBarangs) b.Stok = 0;
            }
            var negLokasis = await _context.BarangLokasis.Where(bl => bl.Stok < 0).ToListAsync();
            if (negLokasis.Any())
            {
                foreach (var bl in negLokasis) bl.Stok = 0;
            }
            if (negBarangs.Any() || negLokasis.Any())
            {
                await _context.SaveChangesAsync();
            }

            var query = _context.Barangs.Include(b => b.Kategori).AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.NamaBarang.Contains(search) || b.KodeBarang.Contains(search));
                ViewBag.Search = search;
            }
            if (kategoriId.HasValue)
            {
                query = query.Where(b => b.KategoriId == kategoriId);
                ViewBag.KategoriId = kategoriId;
            }
            ViewBag.Kategoris = new SelectList(await _context.Kategoris.ToListAsync(), "Id", "NamaKategori", kategoriId);
            return View(await query.OrderBy(b => b.NamaBarang).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> SyncStok(int? id)
        {
            if (id.HasValue && id.Value > 0)
            {
                var newStok = await _stokSync.SyncBarangAsync(id.Value);
                TempData["Success"] = $"Stok & Serial Number berhasil disinkronkan! (Stok saat ini: {newStok})";
            }
            else
            {
                var count = await _stokSync.SyncAllBarangAsync();
                TempData["Success"] = $"Seluruh data stok & Serial Number ({count} barang) berhasil disinkronkan sesuai riwayat transaksi!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> FixNegativeStok()
        {
            var negBarangs = await _context.Barangs.Where(b => b.Stok < 0).ToListAsync();
            foreach (var b in negBarangs) b.Stok = 0;

            var negLokasis = await _context.BarangLokasis.Where(bl => bl.Stok < 0).ToListAsync();
            foreach (var bl in negLokasis) bl.Stok = 0;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Stok negatif berhasil diperbarui ke 0. ({negBarangs.Count} barang, {negLokasis.Count} lokasi diperbaiki)";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> KartuStok(int id)
        {
            var barang = await _context.Barangs.Include(b => b.Kategori).FirstOrDefaultAsync(b => b.Id == id);
            if (barang == null) return NotFound();

            var masuk = await _context.BarangMasuks
                .Where(b => b.BarangId == id)
                .OrderBy(b => b.TanggalMasuk)
                .ToListAsync();

            var keluar = await _context.BarangKeluars
                .Where(b => b.BarangId == id)
                .OrderBy(b => b.TanggalKeluar)
                .ToListAsync();


            ViewBag.Barang = barang;
            ViewBag.BarangMasuk = masuk;
            ViewBag.BarangKeluar = keluar;

            // Build transaction history
            var transactions = new List<dynamic>();
            foreach (var m in masuk)
            {
                transactions.Add(new { Tanggal = m.TanggalMasuk, Tipe = "Masuk", Jumlah = m.Jumlah, Keterangan = m.Keterangan ?? "-" });
            }
            foreach (var k in keluar)
            {
                transactions.Add(new { Tanggal = k.TanggalKeluar, Tipe = "Keluar", Jumlah = k.Jumlah, Keterangan = k.Penerima });
            }

            ViewBag.Transactions = transactions.OrderBy(t => t.Tanggal).ToList();

            return View();
        }
    }
}
