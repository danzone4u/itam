using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;

namespace MyGudang.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class KopSuratController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public KopSuratController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (kop == null)
            {
                kop = new KopSurat();
                _context.KopSurats.Add(kop);
                await _context.SaveChangesAsync();
            }
            return View(kop);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(KopSurat model, IFormFile? LogoFile)
        {
            var kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (kop == null)
            {
                _context.KopSurats.Add(model);
            }
            else
            {
                kop.NamaPerusahaan = model.NamaPerusahaan;
                kop.SubJudul = model.SubJudul;
                kop.Alamat = model.Alamat;
                kop.Telepon = model.Telepon;
                kop.Email = model.Email;
                kop.Website = model.Website;
                kop.NamaPengirim = model.NamaPengirim;
                kop.JabatanPengirim = model.JabatanPengirim;
                kop.TampilkanLogo = model.TampilkanLogo;
            }

            if (LogoFile != null && LogoFile.Length > 0)
            {
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "kop_surat");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string uniqueFileName = "logo_kop_" + Guid.NewGuid().ToString() + Path.GetExtension(LogoFile.FileName);
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await LogoFile.CopyToAsync(fileStream);
                }

                if (kop != null && !string.IsNullOrEmpty(kop.LogoPath))
                {
                    string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, kop.LogoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                if (kop != null)
                {
                    kop.LogoPath = "/uploads/kop_surat/" + uniqueFileName;
                }
                else
                {
                    model.LogoPath = "/uploads/kop_surat/" + uniqueFileName;
                }
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Kop surat berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }
    }
}
