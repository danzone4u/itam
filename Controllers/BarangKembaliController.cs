using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;

namespace itam.Controllers
{
    [Authorize(Roles = "SuperAdmin,AdminGudang")]
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
                .Include(bk => bk.BarangSerials)
                .OrderByDescending(bk => bk.TanggalKembali)
                .ToListAsync();
            return View(data);
        }

        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> Create(int? barangKeluarId)
        {
            var suratJalans = await _context.BarangKeluars
                .Where(bk => bk.NoSuratJalan != null)
                .Select(bk => new { 
                    NoSuratJalan = bk.NoSuratJalan, 
                    Tanggal = bk.TanggalKeluar,
                    Penerima = bk.Penerima
                })
                .Distinct()
                .OrderByDescending(s => s.Tanggal)
                .ToListAsync();

            ViewBag.SuratJalans = suratJalans.Select(s => new SelectListItem {
                Value = s.NoSuratJalan,
                Text = $"{s.NoSuratJalan} - {s.Penerima} ({s.Tanggal:dd/MM/yyyy})"
            }).ToList();

            // Pre-fill from BarangKeluar if provided
            if (barangKeluarId.HasValue)
            {
                var bk = await _context.BarangKeluars.FindAsync(barangKeluarId);
                if (bk != null)
                {
                    ViewBag.PrefilledSuratJalan = bk.NoSuratJalan;
                    ViewBag.PrefilledBarangKeluarId = bk.Id;
                    ViewBag.PrefilledJumlah = bk.Jumlah;
                    ViewBag.PrefilledPenerima = bk.Penerima;
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> Create(BarangKembali model, int[] serialIds)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                _context.Add(model);

                // Update stok barang HANYA jika tindak lanjut = Dikembalikan ke Stok
                var barang = await _context.Barangs.FindAsync(model.BarangId);
                if (barang != null && model.TindakLanjut == "Dikembalikan ke Stok")
                {
                    barang.Stok += model.Jumlah;
                }

                // Deduct from BarangKeluar record
                if (model.BarangKeluarId.HasValue)
                {
                    var bk = await _context.BarangKeluars.FindAsync(model.BarangKeluarId);
                    if (bk != null && bk.Jumlah >= model.Jumlah)
                    {
                        bk.Jumlah -= model.Jumlah;
                    }
                }

                await _context.SaveChangesAsync();

                if (serialIds != null && serialIds.Length > 0)
                {
                    var serialsToUpdate = await _context.BarangSerials.Where(s => serialIds.Contains(s.Id)).ToListAsync();
                    foreach (var sn in serialsToUpdate)
                    {
                        sn.BarangKembaliId = model.Id;
                        if (model.TindakLanjut == "Di Disposal") sn.Status = "Disposal";
                        else if (model.TindakLanjut == "Diperbaiki") sn.Status = "Diperbaiki";
                        else sn.Status = "Tersedia";
                        
                        // Ambil kondisi S/N dari form
                        var condVal = Request.Form[$"snConditions_{sn.Id}"].ToString();
                        
                        // Set kondisi berdasarkan inputan form atau general Kondisi barang
                        if (string.IsNullOrEmpty(condVal))
                        {
                            sn.Kondisi = model.Kondisi == "Baik" ? "Layak Pakai" : "Rusak / Tidak Layak Pakai";
                        }
                        else
                        {
                            sn.Kondisi = condVal;
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Pengembalian barang berhasil dicatat! " + (model.TindakLanjut == "Dikembalikan ke Stok" ? "Stok diperbarui." : (model.TindakLanjut == "Diperbaiki" ? "Barang masuk perbaikan." : "Barang di-disposal."));
                return RedirectToAction(nameof(Index));
            }

            var suratJalans = await _context.BarangKeluars
                .Where(bk => bk.NoSuratJalan != null)
                .Select(bk => new { 
                    NoSuratJalan = bk.NoSuratJalan, 
                    Tanggal = bk.TanggalKeluar,
                    Penerima = bk.Penerima
                })
                .Distinct()
                .OrderByDescending(s => s.Tanggal)
                .ToListAsync();

            ViewBag.SuratJalans = suratJalans.Select(s => new SelectListItem {
                Value = s.NoSuratJalan,
                Text = $"{s.NoSuratJalan} - {s.Penerima} ({s.Tanggal:dd/MM/yyyy})"
            }).ToList();

            if (model.BarangKeluarId.HasValue)
            {
                var bk = await _context.BarangKeluars.FindAsync(model.BarangKeluarId);
                if (bk != null) ViewBag.PrefilledSuratJalan = bk.NoSuratJalan;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.BarangKembalis.Include(bk => bk.BarangSerials).FirstOrDefaultAsync(bk => bk.Id == id);
            if (item != null)
            {
                // Kembalikan stok (kurangi lagi) JIKA sebelumnya dimasukkan ke stok
                if (item.TindakLanjut == "Dikembalikan ke Stok" || item.TindakLanjut == "Selesai Diperbaiki")
                {
                    var barang = await _context.Barangs.FindAsync(item.BarangId);
                    if (barang != null)
                    {
                        barang.Stok -= item.Jumlah;
                        if (barang.Stok < 0) barang.Stok = 0;
                    }
                }

                if (item.BarangKeluarId.HasValue)
                {
                    var bk = await _context.BarangKeluars.FindAsync(item.BarangKeluarId);
                    if (bk != null)
                    {
                        bk.Jumlah += item.Jumlah;
                    }
                }

                // Rollback SNs
                foreach (var sn in item.BarangSerials)
                {
                    sn.BarangKembaliId = null;
                    sn.Status = "Keluar"; 
                    // SN condition might have been different before, but standardizing back to string.Empty or keep as is.
                }

                _context.BarangKembalis.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Data pengembalian berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> BulkDelete(int[] ids)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));
            var items = await _context.BarangKembalis.Include(bk => bk.BarangSerials).Where(b => ids.Contains(b.Id)).ToListAsync();
            foreach (var item in items)
            {
                if (item.TindakLanjut == "Dikembalikan ke Stok" || item.TindakLanjut == "Selesai Diperbaiki")
                {
                    var barang = await _context.Barangs.FindAsync(item.BarangId);
                    if (barang != null) { barang.Stok -= item.Jumlah; if (barang.Stok < 0) barang.Stok = 0; }
                }

                if (item.BarangKeluarId.HasValue)
                {
                    var bk = await _context.BarangKeluars.FindAsync(item.BarangKeluarId);
                    if (bk != null)
                    {
                        bk.Jumlah += item.Jumlah;
                    }
                }

                if (item.BarangSerials != null)
                {
                    foreach (var sn in item.BarangSerials)
                    {
                        sn.BarangKembaliId = null;
                        sn.Status = "Keluar";
                    }
                }
            }
            _context.BarangKembalis.RemoveRange(items);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"{items.Count} data pengembalian berhasil dihapus!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> SelesaiPerbaikan(int id)
        {
            var item = await _context.BarangKembalis.Include(b => b.BarangSerials).FirstOrDefaultAsync(x => x.Id == id);
            if (item != null && item.TindakLanjut == "Diperbaiki")
            {
                var barang = await _context.Barangs.FindAsync(item.BarangId);
                if (barang != null) { barang.Stok += item.Jumlah; }
                
                item.TindakLanjut = "Selesai Diperbaiki";
                foreach (var sn in item.BarangSerials)
                {
                    if (sn.Status == "Diperbaiki") sn.Status = "Tersedia";
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Barang selesai diperbaiki dan telah dikembalikan ke stok gudang.";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> SuratPengembalian(int id)
        {
            var item = await _context.BarangKembalis
                .Include(bk => bk.Barang)
                .Include(bk => bk.BarangKeluar)
                .Include(bk => bk.BarangSerials)
                .FirstOrDefaultAsync(bk => bk.Id == id);
            if (item == null) return NotFound();

            var suratSetting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var count = await _context.BarangKembalis.Where(b => b.Id <= id).CountAsync();

            var kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync() ?? new KopSurat();
            ViewBag.Kop = kop;
            ViewBag.NoSuratKembali = SuratSettingController.GenerateNomorSurat(suratSetting, count, "SK");

            if (item.BarangKeluarId.HasValue)
            {
                var bkCount = await _context.BarangKeluars.Where(b => b.Id <= item.BarangKeluarId.Value).CountAsync();
                ViewBag.RefBast = SuratSettingController.GenerateNomorSurat(suratSetting, bkCount, "STB");
            }

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
                .Where(s => s.Status == "Keluar" && s.BarangKembaliId == null)
                .Select(s => new { id = s.Id, sn = s.SerialNumber })
                .ToList();

            return Json(new { barangId = bk.BarangId, jumlah = bk.Jumlah, penerima = bk.Penerima, serials = serials });
        }

        [HttpGet]
        public async Task<IActionResult> GetItemsBySuratJalan(string noSuratJalan)
        {
            if (string.IsNullOrEmpty(noSuratJalan)) return Json(new object[] { });

            var items = await _context.BarangKeluars
                .Include(b => b.Barang)
                .Where(b => b.NoSuratJalan == noSuratJalan)
                .Select(b => new {
                    id = b.Id,
                    text = b.Barang!.NamaBarang + " - Qty: " + b.Jumlah,
                    barangId = b.BarangId
                })
                .ToListAsync();

            return Json(items);
        }
    }
}
