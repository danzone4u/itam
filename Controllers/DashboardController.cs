using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;

namespace itam.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalBarang = await _context.Barangs.CountAsync();
            ViewBag.TotalBarangMasuk = await _context.BarangMasuks.SumAsync(x => (int?)x.Jumlah) ?? 0;
            ViewBag.TotalBarangKeluar = await _context.BarangKeluars.SumAsync(x => (int?)x.Jumlah) ?? 0;
            ViewBag.StokRendah = await _context.Barangs.CountAsync(b => b.Stok <= b.StokMinimum);

            // Low stock items (per-barang threshold)
            ViewBag.LowStockItems = await _context.Barangs
                .Include(b => b.Kategori)
                .Where(b => b.Stok <= b.StokMinimum)
                .OrderBy(b => b.Stok)
                .Take(10)
                .ToListAsync();

            // Overdue peminjaman
            ViewBag.OverduePeminjaman = await _context.Set<Peminjaman>()
                .Where(p => p.Status == "Dipinjam" && p.TanggalJatuhTempo < DateTime.Now)
                .CountAsync();

            ViewBag.OverdueItems = await _context.Set<Peminjaman>()
                .Include(p => p.Barang)
                .Where(p => p.Status == "Dipinjam" && p.TanggalJatuhTempo < DateTime.Now)
                .OrderBy(p => p.TanggalJatuhTempo)
                .Take(5)
                .ToListAsync();

            // Active chart settings
            ViewBag.Charts = await _context.ChartSettings
                .Where(c => c.Aktif)
                .OrderBy(c => c.Urutan)
                .ToListAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(int months = 6)
        {
            var now = DateTime.Now;
            var monthList = Enumerable.Range(0, months).Select(i => now.AddMonths(-i)).Reverse().ToList();

            var masukData = new List<int>();
            var keluarData = new List<int>();
            var labels = new List<string>();

            foreach (var month in monthList)
            {
                var startOfMonth = new DateTime(month.Year, month.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1);

                masukData.Add(await _context.BarangMasuks
                    .Where(b => b.TanggalMasuk >= startOfMonth && b.TanggalMasuk < endOfMonth)
                    .SumAsync(b => (int?)b.Jumlah) ?? 0);

                keluarData.Add(await _context.BarangKeluars
                    .Where(b => b.TanggalKeluar >= startOfMonth && b.TanggalKeluar < endOfMonth)
                    .SumAsync(b => (int?)b.Jumlah) ?? 0);

                labels.Add(month.ToString("MMM yyyy"));
            }

            // Kategori chart
            var kategoriData = await _context.Barangs
                .Include(b => b.Kategori)
                .GroupBy(b => b.Kategori!.NamaKategori)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .ToListAsync();

            // Stok barang top 10
            var stokData = await _context.Barangs
                .OrderByDescending(b => b.Stok)
                .Take(10)
                .Select(b => new { Label = b.NamaBarang, Value = b.Stok })
                .ToListAsync();

            // Barang paling sering keluar
            var topKeluarData = await _context.BarangKeluars
                .Include(b => b.Barang)
                .GroupBy(b => b.Barang!.NamaBarang)
                .Select(g => new { Label = g.Key, Value = g.Sum(x => x.Jumlah) })
                .OrderByDescending(x => x.Value)
                .Take(10)
                .ToListAsync();

            return Json(new
            {
                labels,
                masuk = masukData,
                keluar = keluarData,
                kategoriLabels = kategoriData.Select(k => k.Label),
                kategoriCounts = kategoriData.Select(k => k.Count),
                stokLabels = stokData.Select(s => s.Label),
                stokValues = stokData.Select(s => s.Value),
                topKeluarLabels = topKeluarData.Select(x => x.Label),
                topKeluarValues = topKeluarData.Select(x => x.Value)
            });
        }
    }
}
