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
                barangMasuk.CreatedAt = DateTime.Now;
                _context.Add(barangMasuk);

                // Update stok barang
                var barang = await _context.Barangs.FindAsync(barangMasuk.BarangId);
                if (barang != null)
                {
                    barang.Stok += barangMasuk.Jumlah;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Barang masuk berhasil ditambahkan!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Barangs = new SelectList(await _context.Barangs.ToListAsync(), "Id", "NamaBarang", barangMasuk.BarangId);
            return View(barangMasuk);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMultiple(int[] barangIds, int[] jumlahs, string[] keterangans, string[] serialNumbers, DateTime tanggalMasuk, string? keteranganGlobal, int? lokasiId)
        {
            if (barangIds == null || barangIds.Length == 0)
            {
                TempData["Error"] = "Pilih minimal 1 barang!";
                ViewBag.Barangs = new SelectList(await _context.Barangs.ToListAsync(), "Id", "NamaBarang");
                ViewBag.Lokasis = new SelectList(await _context.Lokasis.OrderBy(l => l.NamaLokasi).ToListAsync(), "Id", "NamaLokasi");
                return View("Create");
            }

            int count = 0;
            for (int i = 0; i < barangIds.Length; i++)
            {
                if (barangIds[i] <= 0 || jumlahs[i] <= 0) continue;

                var ket = (keterangans != null && i < keterangans.Length && !string.IsNullOrWhiteSpace(keterangans[i]))
                    ? keterangans[i] : keteranganGlobal;

                var snInput = (serialNumbers != null && i < serialNumbers.Length) ? serialNumbers[i] : null;
                var snList = new List<string>();
                if (!string.IsNullOrWhiteSpace(snInput))
                {
                    snList = snInput.Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(s => s.Trim())
                                   .Where(s => !string.IsNullOrEmpty(s))
                                   .ToList();
                }

                int actualJumlah = snList.Count > 0 ? snList.Count : jumlahs[i];

                var bm = new BarangMasuk
                {
                    BarangId = barangIds[i],
                    Jumlah = actualJumlah,
                    TanggalMasuk = tanggalMasuk,
                    Keterangan = ket,
                    LokasiId = (lokasiId.HasValue && lokasiId.Value > 0) ? lokasiId : null,
                    CreatedAt = DateTime.Now
                };
                _context.BarangMasuks.Add(bm);
                await _context.SaveChangesAsync();

                if (snList.Count > 0)
                {
                    foreach (var sn in snList)
                    {
                        _context.BarangSerials.Add(new BarangSerial
                        {
                            BarangId = barangIds[i],
                            SerialNumber = sn,
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
                if (lokasiId.HasValue && lokasiId.Value > 0)
                {
                    var bl = await _context.BarangLokasis
                        .FirstOrDefaultAsync(x => x.BarangId == barangIds[i] && x.LokasiId == lokasiId.Value);
                    if (bl != null)
                        bl.Stok += actualJumlah;
                    else
                        _context.BarangLokasis.Add(new BarangLokasi { BarangId = barangIds[i], LokasiId = lokasiId.Value, Stok = actualJumlah });
                }

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
            var kategoriContoh = await _context.Kategoris.FirstOrDefaultAsync();
            var supplierContoh = await _context.Suppliers.FirstOrDefaultAsync();
            var lokasiContoh = await _context.Lokasis.FirstOrDefaultAsync();

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

                    int supplierId = 0;
                    if (!string.IsNullOrEmpty(supplierNama))
                    {
                        var sup = suppliers.FirstOrDefault(s => s.NamaSupplier.Equals(supplierNama, StringComparison.OrdinalIgnoreCase));
                        if (sup != null) supplierId = sup.Id;
                        else
                        {
                            var newSup = new Supplier { NamaSupplier = supplierNama };
                            _context.Suppliers.Add(newSup);
                            await _context.SaveChangesAsync();
                            suppliers.Add(newSup);
                            supplierId = newSup.Id;
                        }
                    }

                    if (kategoriId > 0 && supplierId > 0)
                    {
                        barang = new Barang
                        {
                            KodeBarang = kodeBarang,
                            NamaBarang = namaBarang,
                            KategoriId = kategoriId,
                            SupplierId = supplierId,
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

                int? lokasiId = null;
                if (!string.IsNullOrEmpty(lokasiNama))
                {
                    if (int.TryParse(lokasiNama, out int lId))
                    {
                        var lok = lokasis.FirstOrDefault(l => l.Id == lId);
                        if (lok != null) lokasiId = lok.Id;
                    }
                    else
                    {
                        var lok = lokasis.FirstOrDefault(l => l.NamaLokasi.Equals(lokasiNama, StringComparison.OrdinalIgnoreCase));
                        if (lok != null) lokasiId = lok.Id;
                    }
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

                var bm = new BarangMasuk
                {
                    BarangId = barang.Id,
                    Jumlah = actualJumlah,
                    TanggalMasuk = tanggalMasuk,
                    Keterangan = ket,
                    LokasiId = lokasiId,
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
                
                if (lokasiId.HasValue && lokasiId.Value > 0)
                {
                    var bl = await _context.BarangLokasis.FirstOrDefaultAsync(x => x.BarangId == barang.Id && x.LokasiId == lokasiId.Value);
                    if (bl != null) bl.Stok += actualJumlah;
                    else _context.BarangLokasis.Add(new BarangLokasi { BarangId = barang.Id, LokasiId = lokasiId.Value, Stok = actualJumlah });
                }

                successCount++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{successCount} baris data berhasil diimport!";
            return RedirectToAction(nameof(Index));
        }
    }
}
