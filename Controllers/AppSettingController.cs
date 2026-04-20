using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using itam.Data;
using itam.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace itam.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class AppSettingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AppSettingController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            var setting = _context.AppSettings.FirstOrDefault();
            if (setting == null)
            {
                setting = new AppSetting { AppName = "IT Asset Management" };
            }
            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(AppSetting model, IFormFile? LogoFile, IFormFile? FaviconFile)
        {
            if (ModelState.IsValid)
            {
                var existingSetting = _context.AppSettings.FirstOrDefault();

                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "settings");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                if (LogoFile != null && LogoFile.Length > 0)
                {
                    string uniqueFileName = "logo_" + Guid.NewGuid().ToString() + Path.GetExtension(LogoFile.FileName);
                    string filePath = Path.Combine(uploadFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await LogoFile.CopyToAsync(fileStream);
                    }
                    model.LogoPath = "uploads/settings/" + uniqueFileName;

                    // Delete old logo
                    if (existingSetting != null && !string.IsNullOrEmpty(existingSetting.LogoPath))
                    {
                        string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, existingSetting.LogoPath);
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }
                }
                else
                {
                    model.LogoPath = existingSetting?.LogoPath;
                }

                if (FaviconFile != null && FaviconFile.Length > 0)
                {
                    string uniqueFileName = "favicon_" + Guid.NewGuid().ToString() + Path.GetExtension(FaviconFile.FileName);
                    string filePath = Path.Combine(uploadFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await FaviconFile.CopyToAsync(fileStream);
                    }
                    model.FaviconPath = "uploads/settings/" + uniqueFileName;

                    // Delete old favicon
                    if (existingSetting != null && !string.IsNullOrEmpty(existingSetting.FaviconPath))
                    {
                        string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, existingSetting.FaviconPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }
                }
                else
                {
                    model.FaviconPath = existingSetting?.FaviconPath;
                }

                if (existingSetting == null)
                {
                    _context.AppSettings.Add(model);
                }
                else
                {
                    existingSetting.AppName = model.AppName;
                    existingSetting.LogoPath = model.LogoPath;
                    existingSetting.FaviconPath = model.FaviconPath;
                    _context.Update(existingSetting);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Pengaturan aplikasi berhasil disimpan.";
                return RedirectToAction(nameof(Index));
            }

            return View("Index", model);
        }
    }
}
