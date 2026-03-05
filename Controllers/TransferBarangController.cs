using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;

namespace itam.Controllers
{
    [Authorize(Roles = "SuperAdmin,AdminGudang")]
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
                .Include(t => t.TransferBarangSerials!).ThenInclude(ts => ts.BarangSerial)
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
            var bls = await _context.BarangLokasis
                .Where(b => b.BarangId == barangId && b.LokasiId == lokasiId).ToListAsync();
            return Json(new { stok = bls.Sum(b => b.Stok) });
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSerials(int barangId)
        {
            var serials = await _context.BarangSerials
                .Where(s => s.BarangId == barangId && s.Status == "Tersedia")
                .Select(s => new {
                    id = s.Id,
                    serialNumber = s.SerialNumber
                })
                .ToListAsync();
            return Json(serials);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransferBarang model, int[] serialIds)
        {
            if (model.DariLokasiId == model.KeLokasiId)
            {
                TempData["Error"] = "Lokasi asal dan tujuan tidak boleh sama!";
                return RedirectToAction(nameof(Create));
            }

            // Cek stok di lokasi asal
            var stokAsalList = await _context.BarangLokasis
                .Where(bl => bl.BarangId == model.BarangId && bl.LokasiId == model.DariLokasiId).ToListAsync();
            var totalStokAsal = stokAsalList.Sum(bl => bl.Stok);

            if (totalStokAsal < model.Jumlah)
            {
                TempData["Error"] = $"Stok di lokasi asal tidak mencukupi! (Tersedia: {totalStokAsal})";
                return RedirectToAction(nameof(Create));
            }

            // Kurangi stok asal
            int sisa = model.Jumlah;
            foreach(var bld in stokAsalList.Where(x => x.Stok > 0).OrderBy(x => x.Id)) {
                if(sisa <= 0) break;
                int deduct = Math.Min(bld.Stok, sisa);
                bld.Stok -= deduct;
                sisa -= deduct;
            }

            // Tambah stok tujuan
            var stokTujuan = await _context.BarangLokasis
                .FirstOrDefaultAsync(bl => bl.BarangId == model.BarangId && bl.LokasiId == model.KeLokasiId && bl.RakKompartemen == null);

            if (stokTujuan == null)
            {
                stokTujuan = new BarangLokasi
                {
                    BarangId = model.BarangId,
                    LokasiId = model.KeLokasiId,
                    Stok = model.Jumlah,
                    RakKompartemen = null
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

            // Save selected serials if any
            if (serialIds != null && serialIds.Length > 0)
            {
                foreach (var sId in serialIds)
                {
                    _context.TransferBarangSerials.Add(new TransferBarangSerial
                    {
                        TransferBarangId = model.Id,
                        BarangSerialId = sId
                    });
                }
                await _context.SaveChangesAsync();
            }

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
                    .FirstOrDefaultAsync(bl => bl.BarangId == item.BarangId && bl.LokasiId == item.DariLokasiId && bl.RakKompartemen == null);
                if (stokAsal != null) stokAsal.Stok += item.Jumlah;
                else _context.BarangLokasis.Add(new BarangLokasi { BarangId = item.BarangId, LokasiId = item.DariLokasiId, Stok = item.Jumlah, RakKompartemen = null });

                var blsTujuan = await _context.BarangLokasis
                    .Where(bl => bl.BarangId == item.BarangId && bl.LokasiId == item.KeLokasiId && bl.Stok > 0)
                    .OrderBy(x => x.Id).ToListAsync();
                int sisa = item.Jumlah;
                foreach(var bld in blsTujuan) {
                    if(sisa <= 0) break;
                    int deduct = Math.Min(bld.Stok, sisa);
                    bld.Stok -= deduct;
                    sisa -= deduct;
                }

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
                    .FirstOrDefaultAsync(bl => bl.BarangId == item.BarangId && bl.LokasiId == item.DariLokasiId && bl.RakKompartemen == null);
                if (stokAsal != null) stokAsal.Stok += item.Jumlah;
                else _context.BarangLokasis.Add(new BarangLokasi { BarangId = item.BarangId, LokasiId = item.DariLokasiId, Stok = item.Jumlah, RakKompartemen = null });

                var blsTujuan = await _context.BarangLokasis
                    .Where(bl => bl.BarangId == item.BarangId && bl.LokasiId == item.KeLokasiId && bl.Stok > 0)
                    .OrderBy(x => x.Id).ToListAsync();
                int sisa = item.Jumlah;
                foreach(var bld in blsTujuan) {
                    if(sisa <= 0) break;
                    int deduct = Math.Min(bld.Stok, sisa);
                    bld.Stok -= deduct;
                    sisa -= deduct;
                }
            }
            _context.TransferBarangs.RemoveRange(items);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"{items.Count} data transfer berhasil dihapus!";
            return RedirectToAction(nameof(Index));
        }
    }
}
