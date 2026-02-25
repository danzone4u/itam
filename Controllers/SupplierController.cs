using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;
using ClosedXML.Excel;

namespace MyGudang.Controllers
{
    [Authorize]
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupplierController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Suppliers.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.NamaSupplier.Contains(search));
                ViewBag.Search = search;
            }
            return View(await query.OrderByDescending(s => s.CreatedAt).ToListAsync());
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                supplier.CreatedAt = DateTime.Now;
                _context.Add(supplier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Supplier berhasil ditambahkan!";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(supplier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Supplier berhasil diperbarui!";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Supplier berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportExcel()
        {
            var data = await _context.Suppliers.OrderBy(s => s.NamaSupplier).ToListAsync();
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Supplier");
            ws.Cell(1, 1).Value = "No";
            ws.Cell(1, 2).Value = "Nama Supplier";
            ws.Cell(1, 3).Value = "Alamat";
            ws.Cell(1, 4).Value = "Telepon";
            ws.Cell(1, 5).Value = "Email";
            ws.Cell(1, 6).Value = "Tanggal Dibuat";
            ws.Range("A1:F1").Style.Font.Bold = true;
            ws.Range("A1:F1").Style.Fill.BackgroundColor = XLColor.LightBlue;

            for (int i = 0; i < data.Count; i++)
            {
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = data[i].NamaSupplier;
                ws.Cell(i + 2, 3).Value = data[i].Alamat;
                ws.Cell(i + 2, 4).Value = data[i].Telepon;
                ws.Cell(i + 2, 5).Value = data[i].Email;
                ws.Cell(i + 2, 6).Value = data[i].CreatedAt.ToString("dd/MM/yyyy");
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Supplier.xlsx");
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

                _context.Suppliers.Add(new Supplier
                {
                    NamaSupplier = nama,
                    Alamat = row.Cell(3).GetString(),
                    Telepon = row.Cell(4).GetString(),
                    Email = row.Cell(5).GetString(),
                    CreatedAt = DateTime.Now
                });
                count++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{count} data supplier berhasil diimport!";
            return RedirectToAction(nameof(Index));
        }
    }
}
