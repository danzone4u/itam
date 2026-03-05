using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;

namespace itam.Controllers
{
    [Authorize]
    public class ArsipController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ArsipController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Arsips.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.NamaDokumen.Contains(search));
                ViewBag.Search = search;
            }
            return View(await query.OrderByDescending(a => a.CreatedAt).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            await PopulateNomorDokumens();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Arsip arsip, IFormFile? dokumenFile)
        {
            if (ModelState.IsValid)
            {
                if (dokumenFile != null && dokumenFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(dokumenFile.FileName);
                    var path = Path.Combine(_env.WebRootPath, "arsip", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using var stream = new FileStream(path, FileMode.Create);
                    await dokumenFile.CopyToAsync(stream);
                    arsip.FilePath = "/arsip/" + fileName;
                    arsip.NamaFile = dokumenFile.FileName;
                }
                arsip.CreatedAt = DateTime.Now;
                _context.Add(arsip);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Arsip berhasil ditambahkan!";
                return RedirectToAction(nameof(Index));
            }
            await PopulateNomorDokumens();
            return View(arsip);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var arsip = await _context.Arsips.FindAsync(id);
            if (arsip == null) return NotFound();
            await PopulateNomorDokumens();
            return View(arsip);
        }

        private async Task PopulateNomorDokumens()
        {
            var noSuratJalan = await _context.BarangKeluars.Where(b => !string.IsNullOrEmpty(b.NoSuratJalan)).Select(b => b.NoSuratJalan!).Distinct().ToListAsync();
            var noPeminjaman = await _context.Peminjamans.Where(p => !string.IsNullOrEmpty(p.NoPeminjaman)).Select(p => p.NoPeminjaman!).Distinct().ToListAsync();
            var existingArsip = await _context.Arsips.Where(a => !string.IsNullOrEmpty(a.NomorDokumen)).Select(a => a.NomorDokumen!).Distinct().ToListAsync();
            
            var allNumbers = noSuratJalan.Concat(noPeminjaman).Concat(existingArsip).Distinct().OrderBy(n => n).ToList();
            ViewBag.NomorDokumens = allNumbers;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Arsip arsip, IFormFile? dokumenFile)
        {
            if (id != arsip.Id) return NotFound();
            if (ModelState.IsValid)
            {
                if (dokumenFile != null && dokumenFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(dokumenFile.FileName);
                    var path = Path.Combine(_env.WebRootPath, "arsip", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using var stream = new FileStream(path, FileMode.Create);
                    await dokumenFile.CopyToAsync(stream);
                    arsip.FilePath = "/arsip/" + fileName;
                }
                _context.Update(arsip);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Arsip berhasil diperbarui!";
                return RedirectToAction(nameof(Index));
            }
            return View(arsip);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var arsip = await _context.Arsips.FindAsync(id);
            if (arsip != null)
            {
                _context.Arsips.Remove(arsip);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Arsip berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Download(int id)
        {
            var arsip = await _context.Arsips.FindAsync(id);
            if (arsip == null || string.IsNullOrEmpty(arsip.FilePath)) return NotFound();

            var filePath = Path.Combine(_env.WebRootPath, arsip.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", arsip.NamaDokumen + Path.GetExtension(arsip.FilePath));
        }
    }
}
