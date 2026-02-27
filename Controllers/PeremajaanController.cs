using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;

namespace MyGudang.Controllers
{
    [Authorize]
    public class PeremajaanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PeremajaanController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.Peremajaans
                .Include(p => p.Barang)
                .Include(p => p.BarangKeluar)
                .Include(p => p.BarangSerials)
                .OrderByDescending(p => p.TanggalPeremajaan)
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
                .Select(bk => new { bk.Id, Display = bk.Barang!.NamaBarang + " - " + bk.Penerima + " (" + bk.TanggalKeluar.ToString("dd/MM/yyyy") + ") Qty:" + bk.Jumlah })
                .ToListAsync();
            ViewBag.BarangKeluars = new SelectList(barangKeluarList, "Id", "Display", barangKeluarId);

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
        public async Task<IActionResult> Create(Peremajaan model, int[] serialIds)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                _context.Add(model);

                // Jika tindak lanjut = Masuk Inventaris → stok bertambah
                if (model.TindakLanjut == "Masuk Inventaris")
                {
                    var barang = await _context.Barangs.FindAsync(model.BarangId);
                    if (barang != null) barang.Stok += model.Jumlah;
                }

                await _context.SaveChangesAsync();

                if (serialIds != null && serialIds.Length > 0)
                {
                    var serialsToUpdate = await _context.BarangSerials.Where(s => serialIds.Contains(s.Id)).ToListAsync();
                    foreach (var sn in serialsToUpdate)
                    {
                        sn.PeremajaanId = model.Id;
                        if (model.TindakLanjut == "Masuk Inventaris")
                        {
                            sn.Status = "Tersedia";
                        }
                        else if (model.TindakLanjut == "Disposal")
                        {
                            sn.Status = "Disposal";
                        }
                        else if (model.TindakLanjut == "Perbaikan")
                        {
                            sn.Status = "Perbaikan";
                        }
                        else
                        {
                            sn.Status = "Rusak";
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Peremajaan barang berhasil dicatat!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Barangs = new SelectList(
                await _context.Barangs.OrderBy(b => b.NamaBarang).ToListAsync(),
                "Id", "NamaBarang", model.BarangId);

            var bkList = await _context.BarangKeluars
                .Include(bk => bk.Barang)
                .OrderByDescending(bk => bk.TanggalKeluar)
                .Select(bk => new { bk.Id, Display = bk.Barang!.NamaBarang + " - " + bk.Penerima + " (" + bk.TanggalKeluar.ToString("dd/MM/yyyy") + ") Qty:" + bk.Jumlah })
                .ToListAsync();
            ViewBag.BarangKeluars = new SelectList(bkList, "Id", "Display", model.BarangKeluarId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Peremajaans.Include(p => p.BarangSerials).FirstOrDefaultAsync(p => p.Id == id);
            if (item != null)
            {
                // Rollback stok if it was added to inventory
                if (item.TindakLanjut == "Masuk Inventaris")
                {
                    var barang = await _context.Barangs.FindAsync(item.BarangId);
                    if (barang != null) { barang.Stok -= item.Jumlah; if (barang.Stok < 0) barang.Stok = 0; }
                }
                
                // Rollback SNs
                foreach (var sn in item.BarangSerials)
                {
                    sn.PeremajaanId = null;
                    sn.Status = "Keluar"; // Return back to previous state since it's connected to BarangKeluar
                }

                _context.Peremajaans.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Data peremajaan berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> SuratPeremajaan(int id)
        {
            var item = await _context.Peremajaans
                .Include(p => p.Barang)
                .Include(p => p.BarangKeluar)
                .Include(p => p.BarangSerials)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (item == null) return NotFound();

            var kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync() ?? new KopSurat();
            ViewBag.Kop = kop;
            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> GetBarangKeluarDetail(int id)
        {
            var bk = await _context.BarangKeluars
                .Include(b => b.Barang)
                .Include(b => b.BarangSerials)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (bk == null) return NotFound();

            var serials = bk.BarangSerials
                .Where(s => s.Status == "Keluar" && s.PeremajaanId == null) // Available for return/renewal
                .Select(s => new { id = s.Id, sn = s.SerialNumber })
                .ToList();

            return Json(new { barangId = bk.BarangId, jumlah = bk.Jumlah, penerima = bk.Penerima, serials = serials });
        }
    }
}
