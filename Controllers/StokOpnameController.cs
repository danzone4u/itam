using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;
using ClosedXML.Excel;

namespace itam.Controllers
{
    [Authorize]
    public class StokOpnameController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StokOpnameController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.StokOpnames.OrderByDescending(s => s.TanggalOpname).ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> Create()
        {
            var barangs = await _context.Barangs.Include(b => b.Kategori).OrderBy(b => b.NamaBarang).ToListAsync();
            ViewBag.Barangs = barangs;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StokOpname opname, int[] BarangId, int[] StokFisik, string[] DetailKeterangan)
        {
            opname.Status = "Draft";
            opname.CreatedAt = DateTime.Now;
            opname.Details = new List<StokOpnameDetail>();

            for (int i = 0; i < BarangId.Length; i++)
            {
                var barang = await _context.Barangs.FindAsync(BarangId[i]);
                if (barang == null) continue;

                opname.Details.Add(new StokOpnameDetail
                {
                    BarangId = BarangId[i],
                    StokSistem = barang.Stok,
                    StokFisik = StokFisik[i],
                    Selisih = StokFisik[i] - barang.Stok,
                    Keterangan = i < DetailKeterangan.Length ? DetailKeterangan[i] : null
                });
            }

            _context.Add(opname);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Stock Opname berhasil dibuat!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Detail(int id)
        {
            var opname = await _context.StokOpnames
                .Include(s => s.Details!)
                    .ThenInclude(d => d.Barang)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (opname == null) return NotFound();
            return View(opname);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finalize(int id)
        {
            var opname = await _context.StokOpnames
                .Include(s => s.Details)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (opname == null) return NotFound();
            if (opname.Status == "Final")
            {
                TempData["Error"] = "Stock Opname sudah final!";
                return RedirectToAction(nameof(Detail), new { id });
            }

            foreach (var detail in opname.Details!)
            {
                var barang = await _context.Barangs.FindAsync(detail.BarangId);
                if (barang != null)
                {
                    barang.Stok = detail.StokFisik;
                }
            }

            opname.Status = "Final";
            await _context.SaveChangesAsync();
            TempData["Success"] = "Stock Opname berhasil difinalisasi! Stok telah diperbarui.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var opname = await _context.StokOpnames.Include(s => s.Details).FirstOrDefaultAsync(s => s.Id == id);
            if (opname != null && opname.Status == "Draft")
            {
                _context.StokOpnames.Remove(opname);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Stock Opname berhasil dihapus!";
            }
            else
            {
                TempData["Error"] = "Stock Opname yang sudah final tidak bisa dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportExcel(int id)
        {
            var opname = await _context.StokOpnames
                .Include(s => s.Details!)
                    .ThenInclude(d => d.Barang)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (opname == null) return NotFound();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Stock Opname");
            ws.Cell(1, 1).Value = $"Laporan Stock Opname - {opname.TanggalOpname:dd/MM/yyyy}";
            ws.Range("A1:G1").Merge().Style.Font.Bold = true;

            ws.Cell(3, 1).Value = "No";
            ws.Cell(3, 2).Value = "Kode Barang";
            ws.Cell(3, 3).Value = "Nama Barang";
            ws.Cell(3, 4).Value = "Merk & Type";
            ws.Cell(3, 5).Value = "Stok Sistem";
            ws.Cell(3, 6).Value = "Stok Fisik";
            ws.Cell(3, 7).Value = "Selisih";
            ws.Cell(3, 8).Value = "Keterangan";
            ws.Range("A3:H3").Style.Font.Bold = true;
            ws.Range("A3:H3").Style.Fill.BackgroundColor = XLColor.LightYellow;

            int row = 4;
            int no = 1;
            foreach (var d in opname.Details!)
            {
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = d.Barang?.KodeBarang;
                ws.Cell(row, 3).Value = d.Barang?.NamaBarang;
                ws.Cell(row, 4).Value = $"{d.Barang?.Merk} {d.Barang?.Type}";
                ws.Cell(row, 5).Value = d.StokSistem;
                ws.Cell(row, 6).Value = d.StokFisik;
                ws.Cell(row, 7).Value = d.Selisih;
                ws.Cell(row, 8).Value = d.Keterangan;
                if (d.Selisih != 0)
                    ws.Range($"A{row}:H{row}").Style.Fill.BackgroundColor = d.Selisih < 0 ? XLColor.LightPink : XLColor.LightGreen;
                row++;
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"StockOpname_{opname.TanggalOpname:yyyyMMdd}.xlsx");
        }
    }
}
