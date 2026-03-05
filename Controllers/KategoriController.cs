using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;
using ClosedXML.Excel;

namespace itam.Controllers
{
    [Authorize]
    public class KategoriController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KategoriController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Kategoris.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(k => k.NamaKategori.Contains(search));
                ViewBag.Search = search;
            }
            return View(await query.OrderByDescending(k => k.CreatedAt).ToListAsync());
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Kategori kategori)
        {
            if (ModelState.IsValid)
            {
                kategori.CreatedAt = DateTime.Now;
                _context.Add(kategori);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Kategori berhasil ditambahkan!";
                return RedirectToAction(nameof(Index));
            }
            return View(kategori);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var kategori = await _context.Kategoris.FindAsync(id);
            if (kategori == null) return NotFound();
            return View(kategori);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Kategori kategori)
        {
            if (id != kategori.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(kategori);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Kategori berhasil diperbarui!";
                return RedirectToAction(nameof(Index));
            }
            return View(kategori);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var kategori = await _context.Kategoris.FindAsync(id);
            if (kategori != null)
            {
                _context.Kategoris.Remove(kategori);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Kategori berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportExcel()
        {
            var data = await _context.Kategoris.OrderBy(k => k.NamaKategori).ToListAsync();
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Kategori");
            ws.Cell(1, 1).Value = "No";
            ws.Cell(1, 2).Value = "Nama Kategori";
            ws.Cell(1, 3).Value = "Deskripsi";
            ws.Cell(1, 4).Value = "Tanggal Dibuat";
            ws.Range("A1:D1").Style.Font.Bold = true;
            ws.Range("A1:D1").Style.Fill.BackgroundColor = XLColor.LightBlue;

            for (int i = 0; i < data.Count; i++)
            {
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = data[i].NamaKategori;
                ws.Cell(i + 2, 3).Value = data[i].Deskripsi;
                ws.Cell(i + 2, 4).Value = data[i].CreatedAt.ToString("dd/MM/yyyy");
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Kategori.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "File tidak boleh kosong!";
                return RedirectToAction(nameof(Index));
            }

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheet(1);
            var rows = ws.RowsUsed().Skip(1);
            int count = 0;

            foreach (var row in rows)
            {
                var nama = row.Cell(2).GetString();
                if (string.IsNullOrWhiteSpace(nama)) continue;

                _context.Kategoris.Add(new Kategori
                {
                    NamaKategori = nama,
                    Deskripsi = row.Cell(3).GetString(),
                    CreatedAt = DateTime.Now
                });
                count++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{count} data kategori berhasil diimport!";
            return RedirectToAction(nameof(Index));
        }
    }
}
