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
                var old = await _context.Kategoris.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id);
                _context.Update(kategori);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Kategori berhasil diperbarui!";
                return RedirectToAction(nameof(Index));
            }
            return View(kategori);
        }

        // ── Ganti prefix semua KodeBarang dalam kategori ini ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenamePrefix(int id, string oldPrefix, string newPrefix)
        {
            if (string.IsNullOrWhiteSpace(newPrefix))
            {
                TempData["Error"] = "Prefix baru tidak boleh kosong.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            newPrefix = newPrefix.Trim().ToUpper();
            oldPrefix = (oldPrefix ?? "").Trim().ToUpper();

            var kategori = await _context.Kategoris.FindAsync(id);
            if (kategori == null) return NotFound();

            // Ambil semua barang di kategori ini yang kodenya diawali prefix lama
            var barangs = await _context.Barangs
                .Where(b => b.KategoriId == id)
                .ToListAsync();

            int count = 0;
            foreach (var b in barangs)
            {
                if (string.IsNullOrEmpty(b.KodeBarang)) continue;

                string oldCode = b.KodeBarang;
                string newCode = oldCode;

                if (!string.IsNullOrEmpty(oldPrefix) && oldCode.StartsWith(oldPrefix + "-", StringComparison.OrdinalIgnoreCase))
                {
                    // Ganti prefix, pertahankan suffix angka
                    newCode = newPrefix + "-" + oldCode.Substring(oldPrefix.Length + 1);
                }
                else
                {
                    // Prefix lama tidak cocok — coba tebak: ambil bagian sebelum "-" pertama
                    var dashIdx = oldCode.IndexOf('-');
                    if (dashIdx > 0)
                        newCode = newPrefix + oldCode.Substring(dashIdx);
                    else
                        newCode = newPrefix + "-" + oldCode;
                }

                if (newCode != oldCode)
                {
                    b.KodeBarang = newCode;
                    b.UpdatedAt  = DateTime.Now;
                    count++;
                }
            }

            // Update juga KodePrefix di kategori
            kategori.KodePrefix = newPrefix;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"✅ Prefix diperbarui ke \"{newPrefix}\". {count} kode barang berhasil diperbarui.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // ── Preview: berapa barang yang akan terpengaruh ──
        [HttpGet]
        public async Task<IActionResult> PreviewRename(int id, string oldPrefix, string newPrefix)
        {
            oldPrefix = (oldPrefix ?? "").Trim().ToUpper();
            newPrefix = (newPrefix ?? "").Trim().ToUpper();

            var barangs = await _context.Barangs
                .Where(b => b.KategoriId == id && b.KodeBarang != null)
                .Select(b => new { b.KodeBarang })
                .ToListAsync();

            var preview = barangs.Select(b =>
            {
                string oldCode = b.KodeBarang ?? "";
                string newCode;
                if (!string.IsNullOrEmpty(oldPrefix) && oldCode.StartsWith(oldPrefix + "-", StringComparison.OrdinalIgnoreCase))
                    newCode = newPrefix + "-" + oldCode.Substring(oldPrefix.Length + 1);
                else
                {
                    var dashIdx = oldCode.IndexOf('-');
                    newCode = dashIdx > 0 ? newPrefix + oldCode.Substring(dashIdx) : newPrefix + "-" + oldCode;
                }
                return new { oldCode, newCode, changed = newCode != oldCode };
            }).Where(x => x.changed).ToList();

            return Json(new { count = preview.Count, items = preview.Take(10) });
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
