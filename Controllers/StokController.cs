using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;

namespace MyGudang.Controllers
{
    [Authorize]
    public class StokController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StokController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, int? kategoriId)
        {
            var query = _context.Barangs.Include(b => b.Kategori).Include(b => b.Supplier).AsQueryable();
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
