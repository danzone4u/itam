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

            // User Specific Statistics
            var username = User.Identity?.Name ?? "";
            ViewBag.UserPermintaanPending = await _context.Permintaans.CountAsync(p => p.PemohonUser == username && p.Status == "Menunggu Approval");
            ViewBag.UserPermintaanDisetujui = await _context.Permintaans.CountAsync(p => p.PemohonUser == username && p.Status == "Disetujui");
            ViewBag.UserPermintaanDitolak = await _context.Permintaans.CountAsync(p => p.PemohonUser == username && p.Status == "Ditolak");
            ViewBag.UserRecentPermintaans = await _context.Permintaans
                .Include(p => p.Lokasi)
                .Include(p => p.Details)
                    .ThenInclude(d => d.Barang)
                .Where(p => p.PemohonUser == username)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Pending Permintaan count & list (for admin)
            ViewBag.PendingPermintaanCount = await _context.Permintaans
                .CountAsync(p => p.Status == "Menunggu Approval");

            ViewBag.PendingPermintaanItems = await _context.Permintaans
                .Include(p => p.Lokasi)
                .Include(p => p.Details)
                    .ThenInclude(d => d.Barang)
                .Where(p => p.Status == "Menunggu Approval")
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

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
            var startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-(months - 1));

            // Optimasi: Gunakan GroupBy di database daripada looping query
            var masuks = await _context.BarangMasuks
                .Where(x => x.TanggalMasuk >= startDate)
                .GroupBy(x => new { x.TanggalMasuk.Year, x.TanggalMasuk.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => (int?)x.Jumlah) ?? 0 })
                .ToListAsync();

            var keluars = await _context.BarangKeluars
                .Where(x => x.TanggalKeluar >= startDate)
                .GroupBy(x => new { x.TanggalKeluar.Year, x.TanggalKeluar.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => (int?)x.Jumlah) ?? 0 })
                .ToListAsync();

            var labels = new List<string>();
            var masukData = new List<int>();
            var keluarData = new List<int>();

            for (int i = 0; i < months; i++)
            {
                var month = startDate.AddMonths(i);
                labels.Add(month.ToString("MMM yyyy"));

                var m = masuks.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month);
                masukData.Add(m?.Total ?? 0);

                var k = keluars.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month);
                keluarData.Add(k?.Total ?? 0);
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
