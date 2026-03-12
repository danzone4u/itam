using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;
using ClosedXML.Excel;

namespace itam.Controllers
{
    [Authorize(Roles = "SuperAdmin,AdminGudang,User")]
    public class BarangKeluarController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BarangKeluarController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.BarangKeluars
                .Include(b => b.Barang)
                .Include(b => b.Lokasi)
                .Include(b => b.BarangSerials)
                .OrderByDescending(b => b.TanggalKeluar)
                .ToListAsync();
            return View(data);
        }

        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Barangs = new SelectList(await _context.Barangs.Where(b => b.Stok > 0).ToListAsync(), "Id", "NamaBarang");
            ViewBag.Lokasis = new SelectList(await _context.Lokasis.OrderBy(l => l.NamaLokasi).ToListAsync(), "Id", "NamaLokasi");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> Create(BarangKeluar barangKeluar)
        {
            if (ModelState.IsValid)
            {
                var barang = await _context.Barangs.FindAsync(barangKeluar.BarangId);
                if (barang == null || barang.Stok < barangKeluar.Jumlah)
                {
                    TempData["Error"] = "Stok tidak mencukupi!";
                    ViewBag.Barangs = new SelectList(await _context.Barangs.Where(b => b.Stok > 0).ToListAsync(), "Id", "NamaBarang", barangKeluar.BarangId);
                    return View(barangKeluar);
                }

                var count = await _context.BarangKeluars.CountAsync() + 1;
                var suratSetting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
                barangKeluar.NoSuratJalan = SuratSettingController.GenerateNomorSurat(suratSetting, count, "SJ");
                barangKeluar.CreatedAt = DateTime.Now;

                barang.Stok -= barangKeluar.Jumlah;
                _context.Add(barangKeluar);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Barang keluar berhasil ditambahkan!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Barangs = new SelectList(await _context.Barangs.Where(b => b.Stok > 0).ToListAsync(), "Id", "NamaBarang", barangKeluar.BarangId);
            return View(barangKeluar);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSerials(int barangId)
        {
            var serials = await _context.BarangSerials
                .Where(s => s.BarangId == barangId && s.Status == "Tersedia")
                .OrderBy(s => s.SerialNumber)
                .Select(s => new { s.Id, s.SerialNumber })
                .ToListAsync();
            return Json(serials);
        }

        [HttpGet]
        public async Task<IActionResult> GetBarangsByLokasi(int? lokasiId)
        {
            if (lokasiId.HasValue && lokasiId.Value > 0)
            {
                var barangs = await _context.BarangLokasis
                    .Include(bl => bl.Barang)
                    .Where(bl => bl.LokasiId == lokasiId.Value && bl.Stok > 0)
                    .GroupBy(bl => new { bl.BarangId, bl.Barang!.NamaBarang })
                    .Select(g => new { value = g.Key.BarangId.ToString(), text = g.Key.NamaBarang })
                    .OrderBy(b => b.text)
                    .ToListAsync();
                return Json(barangs);
            }
            else
            {
                var barangs = await _context.Barangs
                    .Where(b => b.Stok > 0)
                    .Select(b => new { value = b.Id.ToString(), text = b.NamaBarang })
                    .OrderBy(b => b.text)
                    .ToListAsync();
                return Json(barangs);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> CreateMultiple(int[] barangIds, int[] jumlahs, int[] serialIds, DateTime tanggalKeluar,
            string penerima, string? noHpPenerima, string? alamat, string? keteranganGlobal, int? lokasiId, string? pic)
        {
            if ((barangIds == null || barangIds.Length == 0) && (serialIds == null || serialIds.Length == 0) || string.IsNullOrWhiteSpace(penerima))
            {
                TempData["Error"] = "Pilih minimal 1 barang dan isi nama penerima!";
                ViewBag.Barangs = new SelectList(await _context.Barangs.Where(b => b.Stok > 0).ToListAsync(), "Id", "NamaBarang");
                ViewBag.Lokasis = new SelectList(await _context.Lokasis.OrderBy(l => l.NamaLokasi).ToListAsync(), "Id", "NamaLokasi");
                return View("Create");
            }

            var suratSetting2 = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var baseCount = await _context.BarangKeluars.CountAsync();
            
            // Generate ONE shared Surat Jalan number for this transaction
            baseCount++;
            var sharedNoSuratJalan = SuratSettingController.GenerateNomorSurat(suratSetting2, baseCount, "SJ");

            int count = 0;

            // Group serial IDs by barangId
            var serialRecords = new List<BarangSerial>();
            if (serialIds != null && serialIds.Length > 0)
            {
                var idSet = new HashSet<int>(serialIds);
                var allSerials = await _context.BarangSerials.Where(s => s.Status == "Tersedia").ToListAsync();
                serialRecords = allSerials.Where(s => idSet.Contains(s.Id)).ToList();
            }

            // Group by BarangId to create one BarangKeluar per unique barang
            var grouped = new Dictionary<int, List<BarangSerial>>();
            foreach (var sr in serialRecords)
            {
                if (!grouped.ContainsKey(sr.BarangId))
                    grouped[sr.BarangId] = new List<BarangSerial>();
                grouped[sr.BarangId].Add(sr);
            }

            foreach (var kvp in grouped)
            {
                var barang = await _context.Barangs.FindAsync(kvp.Key);
                if (barang == null) continue;

                var bk = new BarangKeluar
                {
                    BarangId = kvp.Key,
                    Jumlah = kvp.Value.Count,
                    TanggalKeluar = tanggalKeluar,
                    Penerima = penerima,
                    NoHpPenerima = noHpPenerima,
                    Alamat = alamat,
                    Keterangan = keteranganGlobal,
                    Pic = pic,
                    NoSuratJalan = sharedNoSuratJalan,
                    LokasiId = (lokasiId.HasValue && lokasiId.Value > 0) ? lokasiId : null,
                    CreatedAt = DateTime.Now
                };
                _context.BarangKeluars.Add(bk);
                await _context.SaveChangesAsync(); // Get bk.Id

                // Update serial statuses
                foreach (var sr in kvp.Value)
                {
                    sr.Status = "Keluar";
                    sr.BarangKeluarId = bk.Id;
                }
                barang.Stok -= kvp.Value.Count;

                // Update BarangLokasi (kurangi stok ruangan)
                if (lokasiId.HasValue && lokasiId.Value > 0)
                {
                    var bls = await _context.BarangLokasis.Where(x => x.BarangId == kvp.Key && x.LokasiId == lokasiId.Value && x.Stok > 0).OrderBy(x => x.Id).ToListAsync();
                    int sisa = kvp.Value.Count;
                    foreach (var bld in bls) {
                        if (sisa <= 0) break;
                        int deduct = Math.Min(bld.Stok, sisa);
                        bld.Stok -= deduct;
                        sisa -= deduct;
                    }
                }

                count++;
            }

            await _context.SaveChangesAsync();

            if (count > 0)
                TempData["Success"] = $"{count} item barang keluar berhasil ditambahkan!";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var bk = await _context.BarangKeluars.Include(b => b.Barang).FirstOrDefaultAsync(b => b.Id == id);
            if (bk == null) return NotFound();
            return View(bk);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> Edit(int id, string penerima, string? alamat, string? keterangan, string? noSuratJalan, string? noHpPenerima)
        {
            var bk = await _context.BarangKeluars.FindAsync(id);
            if (bk == null) return NotFound();

            bk.Penerima = penerima;
            bk.Alamat = alamat;
            bk.Keterangan = keterangan;
            bk.NoHpPenerima = noHpPenerima;
            if (!string.IsNullOrWhiteSpace(noSuratJalan))
                bk.NoSuratJalan = noSuratJalan;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Data berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> Delete(int id)
        {
            var bk = await _context.BarangKeluars.FindAsync(id);
            if (bk != null)
            {
                var barang = await _context.Barangs.FindAsync(bk.BarangId);
                if (barang != null)
                    barang.Stok += bk.Jumlah;

                if (bk.LokasiId.HasValue && bk.LokasiId.Value > 0)
                {
                    var bl = await _context.BarangLokasis.FirstOrDefaultAsync(x => x.BarangId == bk.BarangId && x.LokasiId == bk.LokasiId.Value && x.RakKompartemen == null);
                    if (bl != null) bl.Stok += bk.Jumlah;
                    else _context.BarangLokasis.Add(new BarangLokasi { BarangId = bk.BarangId, LokasiId = bk.LokasiId.Value, Stok = bk.Jumlah, RakKompartemen = null });
                }

                // Restore serial statuses
                var serials = await _context.BarangSerials.Where(s => s.BarangKeluarId == id).ToListAsync();
                foreach (var s in serials)
                {
                    s.Status = "Tersedia";
                    s.BarangKeluarId = null;
                }

                try
                {
                    _context.BarangKeluars.Remove(bk);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Data berhasil dihapus!";
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = "Data tidak dapat dihapus karena masih terkait dengan data lain (misal: Barang Kembali).";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> BulkDelete(int[] ids)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));
            var items = await _context.BarangKeluars.Where(b => ids.Contains(b.Id)).ToListAsync();
            foreach (var bk in items)
            {
                var barang = await _context.Barangs.FindAsync(bk.BarangId);
                if (barang != null) barang.Stok += bk.Jumlah;

                if (bk.LokasiId.HasValue && bk.LokasiId.Value > 0)
                {
                    var bl = await _context.BarangLokasis.FirstOrDefaultAsync(x => x.BarangId == bk.BarangId && x.LokasiId == bk.LokasiId.Value && x.RakKompartemen == null);
                    if (bl != null) bl.Stok += bk.Jumlah;
                    else _context.BarangLokasis.Add(new BarangLokasi { BarangId = bk.BarangId, LokasiId = bk.LokasiId.Value, Stok = bk.Jumlah, RakKompartemen = null });
                }

                var serials = await _context.BarangSerials.Where(s => s.BarangKeluarId == bk.Id).ToListAsync();
                foreach (var s in serials) { s.Status = "Tersedia"; s.BarangKeluarId = null; }
            }

            try
            {
                _context.BarangKeluars.RemoveRange(items);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"{items.Count} data barang keluar berhasil dihapus!";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Beberapa data tidak dapat dihapus karena masih terkait dengan data lain (misal: Barang Kembali).";
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrintSuratJalanBulk(int[] ids)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));

            var data = await _context.BarangKeluars
                .Include(b => b.Barang)
                .Include(b => b.BarangSerials)
                .Where(b => ids.Contains(b.Id))
                .ToListAsync();

            if (!data.Any()) return RedirectToAction(nameof(Index));

            var count = await _context.BarangKeluars.CountAsync(); // Using total count for generation
            var suratSetting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();

            ViewBag.Kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync() ?? new KopSurat();
            ViewBag.NoSuratJalan = SuratSettingController.GenerateNomorSurat(suratSetting, count, "SJ");

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrintBASTBulk(int[] ids)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));

            var data = await _context.BarangKeluars
                .Include(b => b.Barang)
                .Include(b => b.BarangSerials)
                .Where(b => ids.Contains(b.Id))
                .ToListAsync();

            if (!data.Any()) return RedirectToAction(nameof(Index));

            var count = await _context.BarangKeluars.CountAsync(); 
            var suratSetting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();

            ViewBag.Kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync() ?? new KopSurat();
            ViewBag.NoBast = SuratSettingController.GenerateNomorSurat(suratSetting, count, "STB");

            return View(data);
        }

        public async Task<IActionResult> SuratJalan(int id)
        {
            var bk = await _context.BarangKeluars.Include(b => b.Barang).FirstOrDefaultAsync(b => b.Id == id);
            if (bk == null) return NotFound();
            
            var suratSetting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var count = await _context.BarangKeluars.Where(b => b.Id <= id).CountAsync(); // Approximate count for this record
            
            ViewBag.Kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync() ?? new KopSurat();
            ViewBag.Serials = await _context.BarangSerials.Where(s => s.BarangKeluarId == id).Select(s => s.SerialNumber).ToListAsync();
            ViewBag.NoSuratJalan = SuratSettingController.GenerateNomorSurat(suratSetting, count, "SJ");
            
            return View(bk);
        }

        public async Task<IActionResult> SuratTerimaBarang(int id)
        {
            var bk = await _context.BarangKeluars.Include(b => b.Barang).FirstOrDefaultAsync(b => b.Id == id);
            if (bk == null) return NotFound();
            
            var suratSetting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var count = await _context.BarangKeluars.Where(b => b.Id <= id).CountAsync(); // Approximate count for this record
            
            ViewBag.Kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync() ?? new KopSurat();
            ViewBag.Serials = await _context.BarangSerials.Where(s => s.BarangKeluarId == id).Select(s => s.SerialNumber).ToListAsync();
            ViewBag.NoBast = SuratSettingController.GenerateNomorSurat(suratSetting, count, "STB");
            
            return View(bk);
        }

        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> EditSuratJalan(int id)
        {
            var bk = await _context.BarangKeluars.Include(b => b.Barang).FirstOrDefaultAsync(b => b.Id == id);
            if (bk == null) return NotFound();
            return View(bk);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> EditSuratJalan(int id, string penerima, string? alamat, string? keterangan, string? noSuratJalan, string? noHpPenerima)
        {
            var bk = await _context.BarangKeluars.FindAsync(id);
            if (bk == null) return NotFound();
            bk.Penerima = penerima;
            bk.Alamat = alamat;
            bk.Keterangan = keterangan;
            bk.NoHpPenerima = noHpPenerima;
            if (!string.IsNullOrWhiteSpace(noSuratJalan)) bk.NoSuratJalan = noSuratJalan;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Surat Jalan berhasil diperbarui!";
            return RedirectToAction("SuratJalan", new { id });
        }

        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> EditSuratTerima(int id)
        {
            var bk = await _context.BarangKeluars.Include(b => b.Barang).FirstOrDefaultAsync(b => b.Id == id);
            if (bk == null) return NotFound();
            return View(bk);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> EditSuratTerima(int id, string penerima, string? alamat, string? keterangan, string? noHpPenerima)
        {
            var bk = await _context.BarangKeluars.FindAsync(id);
            if (bk == null) return NotFound();
            bk.Penerima = penerima;
            bk.Alamat = alamat;
            bk.Keterangan = keterangan;
            bk.NoHpPenerima = noHpPenerima;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Surat Terima Barang berhasil diperbarui!";
            return RedirectToAction("SuratTerimaBarang", new { id });
        }

        public async Task<IActionResult> ExportExcel()
        {
            var data = await _context.BarangKeluars
                .Include(b => b.Barang)
                .Include(b => b.BarangSerials)
                .OrderByDescending(b => b.TanggalKeluar)
                .ToListAsync();
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Barang Keluar");
            ws.Cell(1, 1).Value = "No";
            ws.Cell(1, 2).Value = "No. Surat Jalan";
            ws.Cell(1, 3).Value = "Tanggal Keluar";
            ws.Cell(1, 4).Value = "Nama Barang";
            ws.Cell(1, 5).Value = "Serial Number";
            ws.Cell(1, 6).Value = "Jumlah";
            ws.Cell(1, 7).Value = "Penerima";
            ws.Cell(1, 8).Value = "Alamat";
            ws.Cell(1, 9).Value = "No HP";
            ws.Cell(1, 10).Value = "Keterangan";
            ws.Range("A1:J1").Style.Font.Bold = true;
            ws.Range("A1:J1").Style.Fill.BackgroundColor = XLColor.LightCoral;

            for (int i = 0; i < data.Count; i++)
            {
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = data[i].NoSuratJalan;
                ws.Cell(i + 2, 3).Value = data[i].TanggalKeluar.ToString("dd/MM/yyyy");
                ws.Cell(i + 2, 4).Value = data[i].Barang?.NamaBarang;
                
                var snList = data[i].BarangSerials?.Where(s => s.SerialNumber != "-").Select(s => s.SerialNumber).ToList() ?? new List<string>();
                ws.Cell(i + 2, 5).Value = snList.Any() ? string.Join(", ", snList) : "-";
                
                ws.Cell(i + 2, 6).Value = data[i].Jumlah;
                ws.Cell(i + 2, 7).Value = data[i].Penerima;
                ws.Cell(i + 2, 8).Value = data[i].Alamat;
                ws.Cell(i + 2, 9).Value = data[i].NoHpPenerima;
                ws.Cell(i + 2, 10).Value = data[i].Keterangan;
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BarangKeluar.xlsx");
        }
    }
}
