using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;

namespace MyGudang.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class LokasiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LokasiController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.Lokasis.OrderBy(l => l.NamaLokasi).ToListAsync();
            // Count stok per lokasi
            foreach (var lok in data)
            {
                var totalStok = await _context.BarangLokasis.Where(bl => bl.LokasiId == lok.Id).SumAsync(bl => bl.Stok);
                ViewData[$"stok_{lok.Id}"] = totalStok;
                var totalBarang = await _context.BarangLokasis.Where(bl => bl.LokasiId == lok.Id && bl.Stok > 0).CountAsync();
                ViewData[$"barang_{lok.Id}"] = totalBarang;
            }
            return View(data);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lokasi model)
        {
            model.CreatedAt = DateTime.Now;
            _context.Lokasis.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Lokasi berhasil ditambahkan!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.Lokasis.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Lokasi model)
        {
            var existing = await _context.Lokasis.FindAsync(model.Id);
            if (existing == null) return NotFound();
            existing.Kode = model.Kode;
            existing.NamaLokasi = model.NamaLokasi;
            existing.Alamat = model.Alamat;
            existing.PenanggungJawab = model.PenanggungJawab;
            existing.NoTelp = model.NoTelp;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Lokasi berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Lokasis.FindAsync(id);
            if (item != null)
            {
                try
                {
                    _context.Lokasis.Remove(item);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Lokasi berhasil dihapus!";
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = "Lokasi tidak dapat dihapus karena masih memiliki data terkait.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Detail(int id)
        {
            var lokasi = await _context.Lokasis.FindAsync(id);
            if (lokasi == null) return NotFound();

            var stokBarang = await _context.BarangLokasis
                .Include(bl => bl.Barang)
                .Where(bl => bl.LokasiId == id && bl.Stok > 0)
                .OrderBy(bl => bl.Barang!.NamaBarang)
                .ToListAsync();

            ViewBag.Lokasi = lokasi;
            return View(stokBarang);
        }
    }
}
