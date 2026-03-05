using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;
using ClosedXML.Excel;
using OfficeOpenXml;
using System.IO;

namespace MyGudang.Controllers
{
    [Authorize]
    public class BarangMasukController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BarangMasukController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.BarangMasuks
                .Include(b => b.Barang)
                .Include(b => b.Lokasi)
                .OrderByDescending(b => b.TanggalMasuk)
                .ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Barangs = new SelectList(await _context.Barangs.ToListAsync(), "Id", "NamaBarang");
            ViewBag.Lokasis = new SelectList(await _context.Lokasis.OrderBy(l => l.NamaLokasi).ToListAsync(), "Id", "NamaLokasi");
            ViewBag.Suppliers = new SelectList(await _context.Suppliers.OrderBy(s => s.NamaSupplier).ToListAsync(), "Id", "NamaSupplier");
            ViewBag.Kategoris = new SelectList(await _context.Kategoris.OrderBy(k => k.NamaKategori).ToListAsync(), "Id", "NamaKategori");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BarangMasuk barangMasuk)
        {
            if (ModelState.IsValid)
            {
                if (!barangMasuk.LokasiId.HasValue || barangMasuk.LokasiId <= 0)
                {
                    barangMasuk.LokasiId = await GetOrCreateLokasiAsync("Ruang IT");
                }
                
                barangMasuk.CreatedAt = DateTime.Now;
                _context.Add(barangMasuk);

                // Update stok barang
                var barang = await _context.Barangs.FindAsync(barangMasuk.BarangId);
                if (barang != null)
                {
                    barang.Stok += barangMasuk.Jumlah;
                }
                
                // Update BarangLokasi (stok per ruangan)
                if (barangMasuk.LokasiId.HasValue && barangMasuk.LokasiId.Value > 0)
                {
                    var bl = await _context.BarangLokasis
                        .FirstOrDefaultAsync(x => x.BarangId == barangMasuk.BarangId && x.LokasiId == barangMasuk.LokasiId.Value);
                    if (bl != null)
                        bl.Stok += barangMasuk.Jumlah;
                    else
                        _context.BarangLokasis.Add(new BarangLokasi { BarangId = barangMasuk.BarangId, LokasiId = barangMasuk.LokasiId.Value, Stok = barangMasuk.Jumlah });
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Barang masuk berhasil ditambahkan!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Barangs = new SelectList(await _context.Barangs.ToListAsync(), "Id", "NamaBarang", barangMasuk.BarangId);
            return View(barangMasuk);
        }

        private async Task<int> GetOrCreateLokasiAsync(string lokasiNama)
        {
            if (string.IsNullOrWhiteSpace(lokasiNama))
                lokasiNama = "Ruang IT";

            var lok = await _context.Lokasis
                .FirstOrDefaultAsync(l => l.NamaLokasi.ToLower() == lokasiNama.ToLower());

            if (lok != null)
            {
                return lok.Id;
            }

            var newLokasi = new Lokasi 
            { 
                NamaLokasi = lokasiNama,
                CreatedAt = DateTime.Now
            };
            
            _context.Lokasis.Add(newLokasi);
            await _context.SaveChangesAsync();
            return newLokasi.Id;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMultiple(int[] barangIds, int[] jumlahs, string[] keterangans, DateTime tanggalMasuk, string? keteranganGlobal, int? lokasiId, int? supplierId)
        {
            if (barangIds == null || barangIds.Length == 0)
            {
                TempData["Error"] = "Pilih minimal 1 barang!";
                ViewBag.Barangs = new SelectList(await _context.Barangs.ToListAsync(), "Id", "NamaBarang");
                ViewBag.Lokasis = new SelectList(await _context.Lokasis.OrderBy(l => l.NamaLokasi).ToListAsync(), "Id", "NamaLokasi");
                ViewBag.Suppliers = new SelectList(await _context.Suppliers.OrderBy(s => s.NamaSupplier).ToListAsync(), "Id", "NamaSupplier");
                return View("Create");
            }

            // Extract the dynamic snRows from Request.Form since jagged arrays are differently bound.
            // Keys come in like "snRows[0]", "snRows[1]"
            var snData = new Dictionary<int, List<string>>();
            var formKeys = Request.Form.Keys.Where(k => k.StartsWith("snRows[")).ToList();
            foreach (var key in formKeys)
            {
                var indexStr = key.Replace("snRows[", "").Replace("]", "");
                if (int.TryParse(indexStr, out int idx))
                {
                    var vals = Request.Form[key].Where(v => !string.IsNullOrEmpty(v)).Select(v => v!).ToList();
                    snData[idx] = vals; // this correlates to the row index
                }
            }

            int count = 0;
            // Since javascript rowIndex sequence might skip numbers if rows are deleted, we will just align it sequentially with barangIds.
            // A more robust way is getting all keys and sorting them or mapping them to the iterations.
            var orderedSnKeys = snData.Keys.OrderBy(k => k).ToList();

            for (int i = 0; i < barangIds.Length; i++)
            {
                if (barangIds[i] <= 0 || jumlahs[i] <= 0) continue;

                var ket = (keterangans != null && i < keterangans.Length && !string.IsNullOrWhiteSpace(keterangans[i]))
                    ? keterangans[i] : keteranganGlobal;

                var snList = new List<string>();
                if (orderedSnKeys.Count > i)
                {
                    int keyIndex = orderedSnKeys[i];
                    snList = snData[keyIndex].Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                }

                int actualJumlah = snList.Count > 0 ? snList.Count : jumlahs[i];

                var bm = new BarangMasuk
                {
                    BarangId = barangIds[i],
                    Jumlah = actualJumlah,
                    TanggalMasuk = tanggalMasuk,
                    Keterangan = ket,
                    LokasiId = (lokasiId.HasValue && lokasiId.Value > 0) ? lokasiId : await GetOrCreateLokasiAsync("Ruang IT"),
                    SupplierId = (supplierId.HasValue && supplierId.Value > 0) ? supplierId : null,
                    CreatedAt = DateTime.Now
                };
                _context.BarangMasuks.Add(bm);
                await _context.SaveChangesAsync();

                if (snList.Count > 0)
                {
                    foreach (var sn in snList)
                    {
                        var finalSn = string.IsNullOrWhiteSpace(sn) || sn == "-" ? "-" : sn.Trim();
                        _context.BarangSerials.Add(new BarangSerial
                        {
                            BarangId = barangIds[i],
                            SerialNumber = finalSn,
                            Status = "Tersedia",
                            BarangMasukId = bm.Id,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
                else
                {
                    for (int j = 0; j < actualJumlah; j++)
                    {
                        _context.BarangSerials.Add(new BarangSerial
                        {
                            BarangId = barangIds[i],
                            SerialNumber = "-",
                            Status = "Tersedia",
                            BarangMasukId = bm.Id,
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                var barang = await _context.Barangs.FindAsync(barangIds[i]);
                if (barang != null) barang.Stok += actualJumlah;

                // Update BarangLokasi (stok per ruangan)
                int finalLokasiId = bm.LokasiId.Value;
                var bl = await _context.BarangLokasis
                    .FirstOrDefaultAsync(x => x.BarangId == barangIds[i] && x.LokasiId == finalLokasiId);
                if (bl != null)
                    bl.Stok += actualJumlah;
                else
                    _context.BarangLokasis.Add(new BarangLokasi { BarangId = barangIds[i], LokasiId = finalLokasiId, Stok = actualJumlah });

                count++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{count} item barang masuk berhasil ditambahkan!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var bm = await _context.BarangMasuks.FindAsync(id);
            if (bm != null)
            {
                var barang = await _context.Barangs.FindAsync(bm.BarangId);
                if (barang != null)
                    barang.Stok -= bm.Jumlah;

                if (bm.LokasiId.HasValue && bm.LokasiId.Value > 0)
                {
                    var bl = await _context.BarangLokasis.FirstOrDefaultAsync(x => x.BarangId == bm.BarangId && x.LokasiId == bm.LokasiId.Value);
                    if (bl != null) bl.Stok -= bm.Jumlah;
                }

                var serials = await _context.BarangSerials.Where(s => s.BarangMasukId == id).ToListAsync();
                _context.BarangSerials.RemoveRange(serials);

                try
                {
                    _context.BarangMasuks.Remove(bm);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Data berhasil dihapus!";
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = "Data tidak dapat dihapus karena masih terkait dengan data lain.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(int[] ids)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));
            var items = await _context.BarangMasuks.Where(b => ids.Contains(b.Id)).ToListAsync();
            foreach (var bm in items)
            {
                var barang = await _context.Barangs.FindAsync(bm.BarangId);
                if (barang != null) barang.Stok -= bm.Jumlah;
                
                if (bm.LokasiId.HasValue && bm.LokasiId.Value > 0)
                {
                    var bl = await _context.BarangLokasis.FirstOrDefaultAsync(x => x.BarangId == bm.BarangId && x.LokasiId == bm.LokasiId.Value);
                    if (bl != null) bl.Stok -= bm.Jumlah;
                }
                
                var serials = await _context.BarangSerials.Where(s => s.BarangMasukId == bm.Id).ToListAsync();
                _context.BarangSerials.RemoveRange(serials);
            }
            
            try
            {
                _context.BarangMasuks.RemoveRange(items);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"{items.Count} data barang masuk berhasil dihapus!";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Beberapa data tidak dapat dihapus karena masih terkait dengan data lain.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrintSuratJalan(int[] ids)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));

            var data = await _context.BarangMasuks
                .Include(b => b.Barang)
                .Include(b => b.Supplier)
                .Include(b => b.BarangSerials)
                .Where(b => ids.Contains(b.Id))
                .ToListAsync();

            if (!data.Any()) return RedirectToAction(nameof(Index));

            var count = await _context.BarangMasuks.CountAsync(); // Using total count for pseudo-numbering or logic
            var suratSetting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            
            ViewBag.Kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync() ?? new KopSurat();
            ViewBag.NoSuratJalan = SuratSettingController.GenerateNomorSurat(suratSetting, count, "SJ");
            
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrintBAST(int[] ids)
        {
            if (ids == null || ids.Length == 0) return RedirectToAction(nameof(Index));

            var data = await _context.BarangMasuks
                .Include(b => b.Barang)
                .Include(b => b.Supplier)
                .Include(b => b.BarangSerials)
                .Where(b => ids.Contains(b.Id))
                .ToListAsync();

            if (!data.Any()) return RedirectToAction(nameof(Index));

            var count = await _context.BarangMasuks.CountAsync(); 
            var suratSetting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            
            ViewBag.Kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync() ?? new KopSurat();
            ViewBag.NoBast = SuratSettingController.GenerateNomorSurat(suratSetting, count, "STB");
            
            return View(data);
        }

        public async Task<IActionResult> ExportExcel()
        {
            var data = await _context.BarangMasuks.Include(b => b.Barang).OrderByDescending(b => b.TanggalMasuk).ToListAsync();
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Barang Masuk");
            ws.Cell(1, 1).Value = "No";
            ws.Cell(1, 2).Value = "Tanggal Masuk";
            ws.Cell(1, 3).Value = "Nama Barang";
            ws.Cell(1, 4).Value = "Jumlah";
            ws.Cell(1, 5).Value = "Keterangan";
            ws.Range("A1:E1").Style.Font.Bold = true;
            ws.Range("A1:E1").Style.Fill.BackgroundColor = XLColor.LightGreen;

            for (int i = 0; i < data.Count; i++)
            {
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = data[i].TanggalMasuk.ToString("dd/MM/yyyy");
                ws.Cell(i + 2, 3).Value = data[i].Barang?.NamaBarang;
                ws.Cell(i + 2, 4).Value = data[i].Jumlah;
                ws.Cell(i + 2, 5).Value = data[i].Keterangan;
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BarangMasuk.xlsx");
        }
        [HttpGet]
        public async Task<IActionResult> DownloadTemplate()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Template Barang Masuk");

            // Header
            worksheet.Cells[1, 1].Value = "Kode Barang (*Wajib)";
            worksheet.Cells[1, 2].Value = "Nama Barang (*Wajib)";
            worksheet.Cells[1, 3].Value = "Kategori";
            worksheet.Cells[1, 4].Value = "Serial Number (Pisahkan dengan koma)";
            worksheet.Cells[1, 5].Value = "Supplier";
            worksheet.Cells[1, 6].Value = "Satuan";
            worksheet.Cells[1, 7].Value = "Jumlah (*Wajib)";
            worksheet.Cells[1, 8].Value = "Tanggal Masuk (DD/MM/YYYY)";
            worksheet.Cells[1, 9].Value = "Keterangan";
            worksheet.Cells[1, 10].Value = "Lokasi Ruangan";

            worksheet.Cells["A1:J1"].Style.Font.Bold = true;

            // Dummy row
            var kategoriContoh = await _context.Kategoris.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var supplierContoh = await _context.Suppliers.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var lokasiContoh = await _context.Lokasis.OrderBy(x => x.Id).FirstOrDefaultAsync();

            worksheet.Cells[2, 1].Value = "BRG-001";
            worksheet.Cells[2, 2].Value = "Contoh Barang";
            worksheet.Cells[2, 3].Value = kategoriContoh?.NamaKategori ?? "IT";
            worksheet.Cells[2, 4].Value = "SN-001, SN-002, SN-003";
            worksheet.Cells[2, 5].Value = supplierContoh?.NamaSupplier ?? "PT Contoh";
            worksheet.Cells[2, 6].Value = "Unit";
            worksheet.Cells[2, 7].Value = 5;
            worksheet.Cells[2, 8].Value = DateTime.Now.ToString("dd/MM/yyyy");
            worksheet.Cells[2, 9].Value = "Stok awal";
            worksheet.Cells[2, 10].Value = lokasiContoh?.NamaLokasi ?? "";

            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_Barang_Masuk.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Pilih file yang akan diimport!";
                return RedirectToAction(nameof(Index));
            }

            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                TempData["Error"] = "File Excel kosong atau format tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            int rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount <= 1)
            {
                TempData["Error"] = "File tidak memiliki data untuk diimport.";
                return RedirectToAction(nameof(Index));
            }

            int successCount = 0;
            var barangs = await _context.Barangs.ToListAsync();
            var lokasis = await _context.Lokasis.ToListAsync();
            var kategoris = await _context.Kategoris.ToListAsync();
            var suppliers = await _context.Suppliers.ToListAsync();

            for (int row = 2; row <= rowCount; row++)
            {
                var kodeBarang = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                var namaBarang = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                var kategoriNama = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                var serialNumberStr = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                var supplierNama = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                var satuan = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                var jumlahStr = worksheet.Cells[row, 7].Value?.ToString()?.Trim();
                var tglStr = worksheet.Cells[row, 8].Value?.ToString()?.Trim();
                var ket = worksheet.Cells[row, 9].Value?.ToString()?.Trim();
                var lokasiNama = worksheet.Cells[row, 10].Value?.ToString()?.Trim();

                if (string.IsNullOrEmpty(kodeBarang) || string.IsNullOrEmpty(jumlahStr)) continue;

                var barang = barangs.FirstOrDefault(b => b.KodeBarang.Equals(kodeBarang, StringComparison.OrdinalIgnoreCase));

                // Auto-create Barang if not exists
                if (barang == null && !string.IsNullOrEmpty(namaBarang))
                {
                    int kategoriId = 0;
                    if (!string.IsNullOrEmpty(kategoriNama))
                    {
                        var kat = kategoris.FirstOrDefault(k => k.NamaKategori.Equals(kategoriNama, StringComparison.OrdinalIgnoreCase));
                        if (kat != null) kategoriId = kat.Id;
                        else
                        {
                            var newKat = new Kategori { NamaKategori = kategoriNama };
                            _context.Kategoris.Add(newKat);
                            await _context.SaveChangesAsync();
                            kategoris.Add(newKat);
                            kategoriId = newKat.Id;
                        }
                    }



                    if (kategoriId > 0)
                    {
                        barang = new Barang
                        {
                            KodeBarang = kodeBarang,
                            NamaBarang = namaBarang,
                            KategoriId = kategoriId,
                            Satuan = !string.IsNullOrEmpty(satuan) ? satuan : "Unit",
                            Stok = 0,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        _context.Barangs.Add(barang);
                        await _context.SaveChangesAsync();
                        barangs.Add(barang);
                    }
                }

                if (barang == null) continue;
                if (!int.TryParse(jumlahStr, out int jumlah) || jumlah <= 0) continue;

                DateTime tanggalMasuk = DateTime.Now;
                if (!string.IsNullOrEmpty(tglStr))
                {
                    if (double.TryParse(tglStr, out double oaDate) && oaDate > 1000) {
                        tanggalMasuk = DateTime.FromOADate(oaDate);
                    } else {
                        DateTime.TryParse(tglStr, out tanggalMasuk);
                    }
                }

                int? finalLokasiId = null;
                if (!string.IsNullOrWhiteSpace(lokasiNama))
                {
                    finalLokasiId = await GetOrCreateLokasiAsync(lokasiNama);
                }
                else
                {
                    finalLokasiId = await GetOrCreateLokasiAsync("Ruang IT");
                }

                var snList = new List<string>();
                if (!string.IsNullOrEmpty(serialNumberStr))
                {
                    snList = serialNumberStr.Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(s => s.Trim())
                                   .Where(s => !string.IsNullOrEmpty(s))
                                   .ToList();
                }

                int actualJumlah = snList.Count > 0 ? snList.Count : jumlah;

                int? bmSupplierId = null;
                if (!string.IsNullOrEmpty(supplierNama))
                {
                    var sup = suppliers.FirstOrDefault(s => s.NamaSupplier.Equals(supplierNama, StringComparison.OrdinalIgnoreCase));
                    if (sup != null) bmSupplierId = sup.Id;
                    else
                    {
                        var newSup = new Supplier { NamaSupplier = supplierNama };
                        _context.Suppliers.Add(newSup);
                        await _context.SaveChangesAsync();
                        suppliers.Add(newSup);
                        bmSupplierId = newSup.Id;
                    }
                }

                var bm = new BarangMasuk
                {
                    BarangId = barang.Id,
                    Jumlah = actualJumlah,
                    TanggalMasuk = tanggalMasuk,
                    Keterangan = ket,
                    LokasiId = finalLokasiId,
                    SupplierId = bmSupplierId,
                    CreatedAt = DateTime.Now
                };

                _context.BarangMasuks.Add(bm);

                if (snList.Count > 0)
                {
                    foreach (var sn in snList)
                    {
                        _context.BarangSerials.Add(new BarangSerial
                        {
                            BarangId = barang.Id,
                            SerialNumber = sn,
                            Status = "Tersedia",
                            BarangMasuk = bm,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
                else
                {
                    for (int j = 0; j < actualJumlah; j++)
                    {
                        _context.BarangSerials.Add(new BarangSerial
                        {
                            BarangId = barang.Id,
                            SerialNumber = "-",
                            Status = "Tersedia",
                            BarangMasuk = bm,
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                barang.Stok += actualJumlah;
                
                if (finalLokasiId.HasValue && finalLokasiId.Value > 0)
                {
                    var bl = await _context.BarangLokasis.FirstOrDefaultAsync(x => x.BarangId == barang.Id && x.LokasiId == finalLokasiId.Value);
                    if (bl != null) bl.Stok += actualJumlah;
                    else _context.BarangLokasis.Add(new BarangLokasi { BarangId = barang.Id, LokasiId = finalLokasiId.Value, Stok = actualJumlah });
                }

                successCount++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{successCount} baris data berhasil diimport!";
            return RedirectToAction(nameof(Index));
        }
    }
}
