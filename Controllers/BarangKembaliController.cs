using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;

namespace MyGudang.Controllers
{
    [Authorize]
    public class BarangKembaliController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BarangKembaliController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.BarangKembalis
                .Include(bk => bk.Barang)
                .Include(bk => bk.BarangKeluar)
                .OrderByDescending(bk => bk.TanggalKembali)
                .ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> Create(int? barangKeluarId)
        {
            ViewBag.Barangs = new SelectList(
                await _context.Barangs.OrderBy(b => b.NamaBarang).ToListAsync(),
                "Id", "NamaBarang");

            var barangKeluarList = await _context.BarangKeluars
                .Include(bk => bk.Barang)
                .OrderByDescending(bk => bk.TanggalKeluar)
                .Select(bk => new { bk.Id, Display = bk.Barang!.NamaBarang + " - " + bk.Penerima + " (" + bk.TanggalKeluar.ToString("dd/MM/yyyy") + ") - Qty: " + bk.Jumlah })
                .ToListAsync();
            ViewBag.BarangKeluars = new SelectList(barangKeluarList, "Id", "Display", barangKeluarId);

            // Pre-fill from BarangKeluar if provided
            if (barangKeluarId.HasValue)
            {
                var bk = await _context.BarangKeluars.FindAsync(barangKeluarId);
                if (bk != null)
                {
                    ViewBag.PrefilledBarangId = bk.BarangId;
                    ViewBag.PrefilledJumlah = bk.Jumlah;
                    ViewBag.PrefilledPenerima = bk.Penerima;
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BarangKembali model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                _context.Add(model);

                // Update stok barang (tambah kembali)
                var barang = await _context.Barangs.FindAsync(model.BarangId);
                if (barang != null)
                {
                    barang.Stok += model.Jumlah;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Pengembalian barang berhasil dicatat! Stok telah diperbarui.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Barangs = new SelectList(
                await _context.Barangs.OrderBy(b => b.NamaBarang).ToListAsync(),
                "Id", "NamaBarang", model.BarangId);

            var barangKeluarList = await _context.BarangKeluars
                .Include(bk => bk.Barang)
                .OrderByDescending(bk => bk.TanggalKeluar)
                .Select(bk => new { bk.Id, Display = bk.Barang!.NamaBarang + " - " + bk.Penerima + " (" + bk.TanggalKeluar.ToString("dd/MM/yyyy") + ") - Qty: " + bk.Jumlah })
                .ToListAsync();
            ViewBag.BarangKeluars = new SelectList(barangKeluarList, "Id", "Display", model.BarangKeluarId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.BarangKembalis.FindAsync(id);
            if (item != null)
            {
                // Kembalikan stok (kurangi lagi)
                var barang = await _context.Barangs.FindAsync(item.BarangId);
                if (barang != null)
                {
                    barang.Stok -= item.Jumlah;
                    if (barang.Stok < 0) barang.Stok = 0;
                }

                _context.BarangKembalis.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Data pengembalian berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(int[] ids)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));
            var items = await _context.BarangKembalis.Where(b => ids.Contains(b.Id)).ToListAsync();
            foreach (var item in items)
            {
                var barang = await _context.Barangs.FindAsync(item.BarangId);
                if (barang != null) { barang.Stok -= item.Jumlah; if (barang.Stok < 0) barang.Stok = 0; }
            }
            _context.BarangKembalis.RemoveRange(items);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"{items.Count} data pengembalian berhasil dihapus!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> SuratPengembalian(int id)
        {
            var item = await _context.BarangKembalis
                .Include(bk => bk.Barang)
                .Include(bk => bk.BarangKeluar)
                .FirstOrDefaultAsync(bk => bk.Id == id);
            if (item == null) return NotFound();

            var suratSetting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var count = await _context.BarangKembalis.Where(b => b.Id <= id).CountAsync();

            var kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync() ?? new KopSurat();
            ViewBag.Kop = kop;
            ViewBag.NoSuratKembali = SuratSettingController.GenerateNomorSurat(suratSetting, count, "SK");

            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> GetBarangKeluarDetail(int id)
        {
            var bk = await _context.BarangKeluars.Include(b => b.Barang).FirstOrDefaultAsync(b => b.Id == id);
            if (bk == null) return NotFound();
            return Json(new { barangId = bk.BarangId, jumlah = bk.Jumlah, penerima = bk.Penerima });
        }
    }
}
