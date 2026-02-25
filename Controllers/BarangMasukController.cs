using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;
using ClosedXML.Excel;

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

                _context.BarangMasuks.Remove(bm);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Data berhasil dihapus!";
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
            }
            _context.BarangMasuks.RemoveRange(items);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"{items.Count} data barang masuk berhasil dihapus!";
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
    }
}
