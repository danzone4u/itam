using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;
using ClosedXML.Excel;
using OfficeOpenXml;
using System.IO;

namespace itam.Controllers
{
    [Authorize(Roles = "SuperAdmin,AdminGudang")]
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
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> CreateMultiple(int[] barangIds, int[] jumlahs, string[] keterangans, DateTime tanggalMasuk, string? keteranganGlobal, int? lokasiId, int? supplierId, string? rakKompartemen)
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
            var orderedSnKeys = snData.Keys.OrderBy(k => k).ToList();

            // ── Kumpulkan semua SN yang akan diinput (non-dash) ──
            var allInputSNs = new List<string>();
            for (int i = 0; i < barangIds.Length; i++)
            {
                if (orderedSnKeys.Count > i)
                {
                    int keyIndex = orderedSnKeys[i];
                    allInputSNs.AddRange(snData[keyIndex].Where(s => !string.IsNullOrWhiteSpace(s) && s.Trim() != "-").Select(s => s.Trim()));
                }
            }

            // ── Cek duplikat dalam batch input sendiri ──
            var batchDuplicates = allInputSNs
                .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            // ── Cek SN yang sudah ada di database ──
            var existingSNs = new List<string>();
            if (allInputSNs.Any())
            {
                var dbSNs = await _context.BarangSerials
                    .Where(s => s.SerialNumber != "-" && allInputSNs.Contains(s.SerialNumber))
                    .Select(s => s.SerialNumber)
                    .ToListAsync();
                existingSNs = dbSNs;
            }

            if (batchDuplicates.Any() || existingSNs.Any())
            {
                var msgs = new List<string>();
                if (batchDuplicates.Any())
                    msgs.Add($"SN duplikat dalam input: {string.Join(", ", batchDuplicates)}");
                if (existingSNs.Any())
                    msgs.Add($"SN sudah ada di database: {string.Join(", ", existingSNs)}");
                TempData["Error"] = "❌ Gagal menyimpan. " + string.Join(" | ", msgs);
                return RedirectToAction(nameof(Create));
            }

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
                var rk = string.IsNullOrWhiteSpace(rakKompartemen) ? null : rakKompartemen.Trim();
                var bl = await _context.BarangLokasis
                    .FirstOrDefaultAsync(x => x.BarangId == barangIds[i] && x.LokasiId == finalLokasiId && x.RakKompartemen == rk);
                if (bl != null)
                    bl.Stok += actualJumlah;
                else
                    _context.BarangLokasis.Add(new BarangLokasi { BarangId = barangIds[i], LokasiId = finalLokasiId, Stok = actualJumlah, RakKompartemen = rk });

                count++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{count} item barang masuk berhasil ditambahkan!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var bm = await _context.BarangMasuks
                .Include(b => b.Barang)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (bm == null) return NotFound();

            var serials = await _context.BarangSerials
                .Where(s => s.BarangMasukId == id)
                .OrderBy(s => s.SerialNumber)
                .Select(s => s.SerialNumber)
                .ToListAsync();

            ViewBag.Lokasis = new SelectList(await _context.Lokasis.OrderBy(l => l.NamaLokasi).ToListAsync(), "Id", "NamaLokasi", bm.LokasiId);
            ViewBag.Suppliers = new SelectList(await _context.Suppliers.OrderBy(s => s.NamaSupplier).ToListAsync(), "Id", "NamaSupplier", bm.SupplierId);
            ViewBag.Serials = serials;

            return View(bm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Edit(int id, DateTime tanggalMasuk, int jumlahBaru, string? keterangan, int? lokasiId, int? supplierId)
        {
            var bm = await _context.BarangMasuks.FindAsync(id);
            if (bm == null) return NotFound();

            var barang = await _context.Barangs.FindAsync(bm.BarangId);
            if (barang == null) return NotFound();
            
            // Handle Serial Numbers from FormData
            var snList = new List<string>();
            var formKeys = Request.Form.Keys.Where(k => k.StartsWith("snRows")).ToList();
            foreach (var key in formKeys)
            {
                var vals = Request.Form[key].Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim()).ToList();
                snList.AddRange(vals);
            }

            int actualJumlah = snList.Count > 0 ? snList.Count : jumlahBaru;
            if (actualJumlah <= 0) actualJumlah = bm.Jumlah; // fallback

            // 1. Calculate difference in stock 
            int selisih = actualJumlah - bm.Jumlah;

            // 2. Remove old serials for this BarangMasuk
            var oldSerials = await _context.BarangSerials.Where(s => s.BarangMasukId == id).ToListAsync();
            _context.BarangSerials.RemoveRange(oldSerials);

            // 3. Update BarangMasuk record
            int? finalLokasiId = (lokasiId.HasValue && lokasiId.Value > 0) ? lokasiId : null;
            if (!finalLokasiId.HasValue) finalLokasiId = await GetOrCreateLokasiAsync("Ruang IT");
            
            // check if lokasi changed
            if(bm.LokasiId != finalLokasiId)
            {
               // restore old lokasi
               if(bm.LokasiId.HasValue) {
                   var bls = await _context.BarangLokasis.Where(x => x.BarangId == bm.BarangId && x.LokasiId == bm.LokasiId.Value && x.Stok > 0).OrderBy(x => x.Id).ToListAsync();
                   int sisa = bm.Jumlah;
                   foreach (var bld in bls) {
                       if (sisa <= 0) break;
                       int deduct = Math.Min(bld.Stok, sisa);
                       bld.Stok -= deduct;
                       sisa -= deduct;
                   }
               }
               // add to new lokasi
               var newBl = await _context.BarangLokasis.FirstOrDefaultAsync(x => x.BarangId == bm.BarangId && x.LokasiId == finalLokasiId.Value && x.RakKompartemen == null);
               if(newBl != null) newBl.Stok += actualJumlah;
               else _context.BarangLokasis.Add(new BarangLokasi { BarangId = bm.BarangId, LokasiId = finalLokasiId.Value, Stok = actualJumlah, RakKompartemen = null });
            }
            else 
            {
               // lokasi is the same, just update difference
               // We add/deduct the difference to/from RakKompartemen = null as fallback
               if (selisih > 0) {
                   var bl = await _context.BarangLokasis.FirstOrDefaultAsync(x => x.BarangId == bm.BarangId && x.LokasiId == finalLokasiId.Value && x.RakKompartemen == null);
                   if (bl != null) bl.Stok += selisih;
                   else _context.BarangLokasis.Add(new BarangLokasi { BarangId = bm.BarangId, LokasiId = finalLokasiId.Value, Stok = selisih, RakKompartemen = null });
               } else if (selisih < 0) {
                   int sisa = Math.Abs(selisih);
                   var bls = await _context.BarangLokasis.Where(x => x.BarangId == bm.BarangId && x.LokasiId == finalLokasiId.Value && x.Stok > 0).OrderBy(x => x.Id).ToListAsync();
                   foreach (var bld in bls) {
                       if (sisa <= 0) break;
                       int deduct = Math.Min(bld.Stok, sisa);
                       bld.Stok -= deduct;
                       sisa -= deduct;
                   }
               }
            }

            barang.Stok += selisih;
            bm.Jumlah = actualJumlah;
            bm.TanggalMasuk = tanggalMasuk;
            bm.Keterangan = keterangan;
            bm.LokasiId = finalLokasiId;
            bm.SupplierId = (supplierId.HasValue && supplierId.Value > 0) ? supplierId : null;

            _context.Update(bm);

            // 4. Insert new serials
            if (snList.Count > 0)
            {
                foreach (var sn in snList)
                {
                    _context.BarangSerials.Add(new BarangSerial
                    {
                        BarangId = bm.BarangId,
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
                        BarangId = bm.BarangId,
                        SerialNumber = "-",
                        Status = "Tersedia",
                        BarangMasukId = bm.Id,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Data barang masuk berhasil diperbarui!";
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
                   var bls = await _context.BarangLokasis.Where(x => x.BarangId == bm.BarangId && x.LokasiId == bm.LokasiId.Value && x.Stok > 0).OrderBy(x => x.Id).ToListAsync();
                   int sisa = bm.Jumlah;
                   foreach (var bld in bls) {
                       if (sisa <= 0) break;
                       int deduct = Math.Min(bld.Stok, sisa);
                       bld.Stok -= deduct;
                       sisa -= deduct;
                   }
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
                   var bls = await _context.BarangLokasis.Where(x => x.BarangId == bm.BarangId && x.LokasiId == bm.LokasiId.Value && x.Stok > 0).OrderBy(x => x.Id).ToListAsync();
                   int sisa = bm.Jumlah;
                   foreach (var bld in bls) {
                       if (sisa <= 0) break;
                       int deduct = Math.Min(bld.Stok, sisa);
                       bld.Stok -= deduct;
                       sisa -= deduct;
                   }
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

            // === LEGEND ROW (row 1) ===
            worksheet.Cells[1, 1].Value = "KETERANGAN: Kolom berlatar MERAH = WAJIB diisi | Kolom berlatar ABU-ABU = Opsional";
            worksheet.Cells[1, 1, 1, 13].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(52, 73, 94));
            worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // === HEADER ROW (row 2) ===
            // Required columns
            var requiredCols = new[] { 2 };
            var headers = new Dictionary<int, string>
            {
                {1,  "Kode Barang (Otomatis jika kosong)"},
                {2,  "Nama Barang *"},
                {3,  "Kategori"},
                {4,  "Serial Number (Pisahkan dengan koma)"},
                {5,  "Supplier"},
                {6,  "Satuan"},
                {7,  "Jumlah (Opsional, ikuti SN jika ada)"},
                {8,  "Tanggal Masuk (DD/MM/YYYY)"},
                {9,  "Keterangan"},
                {10, "Lokasi Ruangan"},
                {11, "Rak / Kompartemen"},
            };

            foreach (var h in headers)
            {
                var cell = worksheet.Cells[2, h.Key];
                cell.Value = h.Value;
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;

                if (requiredCols.Contains(h.Key))
                {
                    // Required: red background
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(192, 0, 0));
                }
                else
                {
                    // Optional: dark grey background
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(89, 89, 89));
                }

                cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // === DUMMY DATA ROW (row 3) ===
            var kategoriContoh = await _context.Kategoris.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var supplierContoh = await _context.Suppliers.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var lokasiContoh   = await _context.Lokasis.OrderBy(x => x.Id).FirstOrDefaultAsync();

            worksheet.Cells[3, 1].Value  = "BRG-001";
            worksheet.Cells[3, 2].Value  = "Contoh Barang";
            worksheet.Cells[3, 3].Value  = kategoriContoh?.NamaKategori ?? "IT";
            worksheet.Cells[3, 4].Value  = "SN-001, SN-002, SN-003";
            worksheet.Cells[3, 5].Value  = supplierContoh?.NamaSupplier ?? "PT Contoh";
            worksheet.Cells[3, 6].Value  = "Unit";
            worksheet.Cells[3, 7].Value  = 3;
            worksheet.Cells[3, 8].Value  = DateTime.Now.ToString("dd/MM/yyyy");
            worksheet.Cells[3, 9].Value  = "Stok awal";
            worksheet.Cells[3, 10].Value = lokasiContoh?.NamaLokasi ?? "";
            worksheet.Cells[3, 11].Value = "Rak A / Lemari 1";

            // style dummy row: light yellow bg
            var dummyRange = worksheet.Cells[3, 1, 3, 11];
            dummyRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            dummyRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 255, 204));
            dummyRange.Style.Font.Italic = true;
            foreach (var cell in dummyRange)
                cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

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
            if (rowCount <= 2)
            {
                TempData["Error"] = "File tidak memiliki data untuk diimport.";
                return RedirectToAction(nameof(Index));
            }

            var barangs    = await _context.Barangs.ToListAsync();
            var lokasis    = await _context.Lokasis.ToListAsync();
            var kategoris  = await _context.Kategoris.ToListAsync();
            var suppliers  = await _context.Suppliers.ToListAsync();

            // ── STEP 1: Read all rows into staging list ──────────────────────
            var staged = new List<(
                string KodeBarang, string NamaBarang,
                string? KategoriNama, List<string> SNs, string? SupplierNama, string? Satuan,
                int Jumlah, DateTime Tanggal, string? Ket, string? LokasiNama, string? Rak
            )>();

            for (int row = 3; row <= rowCount; row++)
            {
                var kodeBarang = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                var namaBarang = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                
                if (string.IsNullOrEmpty(kodeBarang) && string.IsNullOrEmpty(namaBarang)) continue;
                kodeBarang ??= "";

                var kategoriNama   = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                var serialNumberStr= worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                var supplierNama   = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                var satuan         = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                var jumlahStr      = worksheet.Cells[row, 7].Value?.ToString()?.Trim();
                var tglStr         = worksheet.Cells[row, 8].Value?.ToString()?.Trim();
                var ket            = worksheet.Cells[row, 9].Value?.ToString()?.Trim();
                var lokasiNama     = worksheet.Cells[row, 10].Value?.ToString()?.Trim();
                var rakKompartemen = worksheet.Cells[row, 11].Value?.ToString()?.Trim();

                // Parse SNs from this row
                var snList = new List<string>();
                if (!string.IsNullOrEmpty(serialNumberStr))
                    snList = serialNumberStr.Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

                // Parse jumlah (fallback to SN count)
                int.TryParse(jumlahStr, out int jumlah);
                int resolvedJumlah = snList.Count > 0 ? snList.Count : (jumlah > 0 ? jumlah : 1);

                // Parse tanggal
                DateTime tanggal = DateTime.Now;
                if (!string.IsNullOrEmpty(tglStr))
                {
                    if (double.TryParse(tglStr, out double oaDate) && oaDate > 1000)
                        tanggal = DateTime.FromOADate(oaDate);
                    else
                        DateTime.TryParse(tglStr, out tanggal);
                }

                staged.Add((kodeBarang, namaBarang ?? "", kategoriNama,
                            snList, supplierNama, satuan, resolvedJumlah, tanggal, ket,
                            lokasiNama, rakKompartemen));
            }

            // ── STEP 2: Group rows by Kode Barang (or NamaBarang jika kode kosong) + Lokasi + Tanggal + Supplier ──
            // Rows with the same combination are merged into one BarangMasuk.
            var groups = staged.GroupBy(r => new {
                KodeGroup = string.IsNullOrEmpty(r.KodeBarang)
                    ? $"NEW_{r.NamaBarang}"
                    : r.KodeBarang,
                Lokasi  = r.LokasiNama ?? "",
                Tgl     = r.Tanggal.Date,
                Supplier= r.SupplierNama ?? "",
                Rak     = r.Rak ?? ""
            }).ToList();

            int successCount = 0;
            var nextNumbers = new Dictionary<string, int>();

            // ── Cek duplikat SN sebelum proses simpan ──
            var allImportSNs = staged
                .SelectMany(r => r.SNs)
                .Where(s => !string.IsNullOrWhiteSpace(s) && s.Trim() != "-")
                .Select(s => s.Trim())
                .ToList();

            if (allImportSNs.Any())
            {
                // Duplikat dalam file Excel sendiri
                var fileDupes = allImportSNs
                    .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                // SN yang sudah ada di database
                var dbExisting = await _context.BarangSerials
                    .Where(s => s.SerialNumber != "-" && allImportSNs.Contains(s.SerialNumber))
                    .Select(s => s.SerialNumber)
                    .Distinct()
                    .ToListAsync();

                if (fileDupes.Any() || dbExisting.Any())
                {
                    var msgs = new List<string>();
                    if (fileDupes.Any())    msgs.Add($"SN duplikat dalam file: {string.Join(", ", fileDupes)}");
                    if (dbExisting.Any())   msgs.Add($"SN sudah ada di database: {string.Join(", ", dbExisting)}");
                    TempData["Error"] = "❌ Import dibatalkan. " + string.Join(" | ", msgs);
                    return RedirectToAction(nameof(Index));
                }
            }

            foreach (var grp in groups)
            {
                var first = grp.First();

                // ── Resolve / create Barang ──
                Barang? barang = null;
                if (!string.IsNullOrEmpty(first.KodeBarang))
                {
                    barang = barangs.FirstOrDefault(b => b.KodeBarang.Equals(first.KodeBarang, StringComparison.OrdinalIgnoreCase));
                }

                if (barang == null && !string.IsNullOrEmpty(first.NamaBarang))
                {
                    // ── Try to find existing by NamaBarang first ──
                    barang = barangs.FirstOrDefault(b =>
                        b.NamaBarang.Equals(first.NamaBarang, StringComparison.OrdinalIgnoreCase));

                    // Also check DB in case it's not in our in-memory list yet
                    if (barang == null)
                    {
                        barang = await _context.Barangs
                            .FirstOrDefaultAsync(b => b.NamaBarang.ToLower() == first.NamaBarang.ToLower());
                        if (barang != null) barangs.Add(barang);
                    }

                    // ── If still not found, create new Barang ──
                    if (barang == null)
                    {
                        int kategoriId = 0;
                        Kategori? targetKategori = null;
                        if (!string.IsNullOrEmpty(first.KategoriNama))
                        {
                            var kat = kategoris.FirstOrDefault(k =>
                                k.NamaKategori.Equals(first.KategoriNama, StringComparison.OrdinalIgnoreCase));
                            if (kat != null) {
                                kategoriId = kat.Id;
                                targetKategori = kat;
                            }
                            else
                            {
                                var newKat = new Kategori { NamaKategori = first.KategoriNama };
                                _context.Kategoris.Add(newKat);
                                await _context.SaveChangesAsync();
                                kategoris.Add(newKat);
                                kategoriId = newKat.Id;
                                targetKategori = newKat;
                            }
                        }

                        if (kategoriId > 0 && targetKategori != null)
                        {
                            string finalKode = first.KodeBarang;
                            if (string.IsNullOrEmpty(finalKode))
                            {
                                var words = targetKategori.NamaKategori.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                var prefix = !string.IsNullOrWhiteSpace(targetKategori.KodePrefix)
                                    ? targetKategori.KodePrefix.ToUpper().Trim()
                                    : words.Length > 1
                                        ? string.Concat(words.Select(w => w[0])).ToUpper()
                                        : targetKategori.NamaKategori.Length >= 3
                                            ? targetKategori.NamaKategori.Substring(0, 3).ToUpper()
                                            : targetKategori.NamaKategori.ToUpper();

                                if (!nextNumbers.ContainsKey(prefix))
                                {
                                    var lastBarang = await _context.Barangs
                                        .Where(b => b.KategoriId == kategoriId && b.KodeBarang.StartsWith(prefix + "-"))
                                        .OrderByDescending(b => b.KodeBarang)
                                        .FirstOrDefaultAsync();

                                    int nextNum = 1;
                                    if (lastBarang != null)
                                    {
                                        var parts = lastBarang.KodeBarang.Split('-');
                                        if (parts.Length > 1 && int.TryParse(parts.Last(), out int num))
                                            nextNum = num + 1;
                                    }
                                    nextNumbers[prefix] = nextNum;
                                }

                                finalKode = $"{prefix}-{nextNumbers[prefix]:D3}";
                                nextNumbers[prefix]++;
                            }

                            barang = new Barang
                            {
                                KodeBarang = finalKode,
                                NamaBarang = first.NamaBarang,
                                KategoriId = kategoriId,
                                Satuan     = !string.IsNullOrEmpty(first.Satuan) ? first.Satuan : "Unit",
                                Stok       = 0,
                                CreatedAt  = DateTime.Now,
                                UpdatedAt  = DateTime.Now
                            };
                            _context.Barangs.Add(barang);
                            await _context.SaveChangesAsync();
                            barangs.Add(barang);
                        }
                    }
                }

                if (barang == null) continue;

                // ── Resolve Lokasi & Supplier ──
                int? finalLokasiId = null;
                finalLokasiId = !string.IsNullOrWhiteSpace(grp.Key.Lokasi)
                    ? await GetOrCreateLokasiAsync(grp.Key.Lokasi)
                    : await GetOrCreateLokasiAsync("Ruang IT");

                int? bmSupplierId = null;
                if (!string.IsNullOrEmpty(grp.Key.Supplier))
                {
                    var sup = suppliers.FirstOrDefault(s =>
                        s.NamaSupplier.Equals(grp.Key.Supplier, StringComparison.OrdinalIgnoreCase));
                    if (sup != null) bmSupplierId = sup.Id;
                    else
                    {
                        var newSup = new Supplier { NamaSupplier = grp.Key.Supplier };
                        _context.Suppliers.Add(newSup);
                        await _context.SaveChangesAsync();
                        suppliers.Add(newSup);
                        bmSupplierId = newSup.Id;
                    }
                }

                // ── Merge all SNs from all rows in this group ──
                var allSNs    = grp.SelectMany(r => r.SNs).Where(s => !string.IsNullOrEmpty(s)).ToList();
                int totalJumlah = allSNs.Count > 0 ? allSNs.Count : grp.Sum(r => r.Jumlah);

                var bm = new BarangMasuk
                {
                    BarangId    = barang.Id,
                    Jumlah      = totalJumlah,
                    TanggalMasuk= first.Tanggal,
                    Keterangan  = first.Ket,
                    LokasiId    = finalLokasiId,
                    SupplierId  = bmSupplierId,
                    CreatedAt   = DateTime.Now
                };
                _context.BarangMasuks.Add(bm);

                if (allSNs.Count > 0)
                {
                    foreach (var sn in allSNs)
                        _context.BarangSerials.Add(new BarangSerial
                        {
                            BarangId = barang.Id, SerialNumber = sn,
                            Status = "Tersedia", BarangMasuk = bm, CreatedAt = DateTime.Now
                        });
                }
                else
                {
                    for (int j = 0; j < totalJumlah; j++)
                        _context.BarangSerials.Add(new BarangSerial
                        {
                            BarangId = barang.Id, SerialNumber = "-",
                            Status = "Tersedia", BarangMasuk = bm, CreatedAt = DateTime.Now
                        });
                }

                barang.Stok += totalJumlah;

                if (finalLokasiId.HasValue && finalLokasiId.Value > 0)
                {
                    var rk = string.IsNullOrWhiteSpace(grp.Key.Rak) ? null : grp.Key.Rak;
                    var bl = await _context.BarangLokasis.FirstOrDefaultAsync(x =>
                        x.BarangId == barang.Id && x.LokasiId == finalLokasiId.Value && x.RakKompartemen == rk);
                    if (bl != null) bl.Stok += totalJumlah;
                    else _context.BarangLokasis.Add(new BarangLokasi
                    {
                        BarangId = barang.Id, LokasiId = finalLokasiId.Value,
                        Stok = totalJumlah, RakKompartemen = rk
                    });
                }

                successCount++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{successCount} entri Barang Masuk berhasil diimport (dari {staged.Count} baris Excel)!";
            return RedirectToAction(nameof(Index));
        }
    }
}
