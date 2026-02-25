using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;

namespace MyGudang.Controllers
{
    [Authorize]
    public class TransferBarangController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransferBarangController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.TransferBarangs
                .Include(t => t.Barang)
                .Include(t => t.DariLokasi)
                .Include(t => t.KeLokasi)
                .OrderByDescending(t => t.TanggalTransfer)
                .ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Barangs = await _context.Barangs.OrderBy(b => b.NamaBarang).ToListAsync();
            ViewBag.Lokasis = new SelectList(await _context.Lokasis.OrderBy(l => l.NamaLokasi).ToListAsync(), "Id", "NamaLokasi");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStokLokasi(int barangId, int lokasiId)
        {
            var bl = await _context.BarangLokasis
                .FirstOrDefaultAsync(b => b.BarangId == barangId && b.LokasiId == lokasiId);
            return Json(new { stok = bl?.Stok ?? 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransferBarang model)
        {
            if (model.DariLokasiId == model.KeLokasiId)
            {
                TempData["Error"] = "Lokasi asal dan tujuan tidak boleh sama!";
                return RedirectToAction(nameof(Create));
            }

            // Cek stok di lokasi asal
            var stokAsal = await _context.BarangLokasis
                .FirstOrDefaultAsync(bl => bl.BarangId == model.BarangId && bl.LokasiId == model.DariLokasiId);

            if (stokAsal == null || stokAsal.Stok < model.Jumlah)
            {
                TempData["Error"] = $"Stok di lokasi asal tidak mencukupi! (Tersedia: {stokAsal?.Stok ?? 0})";
                return RedirectToAction(nameof(Create));
            }

            // Kurangi stok asal
            stokAsal.Stok -= model.Jumlah;

            // Tambah stok tujuan
            var stokTujuan = await _context.BarangLokasis
                .FirstOrDefaultAsync(bl => bl.BarangId == model.BarangId && bl.LokasiId == model.KeLokasiId);

            if (stokTujuan == null)
            {
                stokTujuan = new BarangLokasi
                {
                    BarangId = model.BarangId,
                    LokasiId = model.KeLokasiId,
                    Stok = model.Jumlah
                };
                _context.BarangLokasis.Add(stokTujuan);
            }
            else
            {
                stokTujuan.Stok += model.Jumlah;
            }

            // Generate nomor transfer
            var count = await _context.TransferBarangs.CountAsync() + 1;
            model.NoTransfer = $"TRF-{DateTime.Now:yyyyMMdd}-{count:D4}";
            model.CreatedAt = DateTime.Now;
            _context.TransferBarangs.Add(model);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Transfer barang berhasil!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.TransferBarangs.FindAsync(id);
            if (item != null)
            {
                // Reverse: kembalikan stok asal, kurangi tujuan
                var stokAsal = await _context.BarangLokasis
                    .FirstOrDefaultAsync(bl => bl.BarangId == item.BarangId && bl.LokasiId == item.DariLokasiId);
                var stokTujuan = await _context.BarangLokasis
                    .FirstOrDefaultAsync(bl => bl.BarangId == item.BarangId && bl.LokasiId == item.KeLokasiId);

                if (stokAsal != null) stokAsal.Stok += item.Jumlah;
                if (stokTujuan != null) stokTujuan.Stok -= item.Jumlah;

                _context.TransferBarangs.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Data transfer berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(int[] ids)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));
            var items = await _context.TransferBarangs.Where(t => ids.Contains(t.Id)).ToListAsync();
            foreach (var item in items)
            {
                var stokAsal = await _context.BarangLokasis
                    .FirstOrDefaultAsync(bl => bl.BarangId == item.BarangId && bl.LokasiId == item.DariLokasiId);
                var stokTujuan = await _context.BarangLokasis
                    .FirstOrDefaultAsync(bl => bl.BarangId == item.BarangId && bl.LokasiId == item.KeLokasiId);
                if (stokAsal != null) stokAsal.Stok += item.Jumlah;
                if (stokTujuan != null) stokTujuan.Stok -= item.Jumlah;
            }
            _context.TransferBarangs.RemoveRange(items);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"{items.Count} data transfer berhasil dihapus!";
            return RedirectToAction(nameof(Index));
        }
    }
}
