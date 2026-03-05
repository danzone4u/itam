using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;

namespace itam.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class ChartSettingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChartSettingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.ChartSettings.OrderBy(c => c.Urutan).ToListAsync();
            return View(data);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChartSetting setting)
        {
            if (ModelState.IsValid)
            {
                setting.CreatedAt = DateTime.Now;
                _context.Add(setting);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Chart berhasil ditambahkan!";
                return RedirectToAction(nameof(Index));
            }
            return View(setting);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var setting = await _context.ChartSettings.FindAsync(id);
            if (setting == null) return NotFound();
            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ChartSetting setting)
        {
            if (id != setting.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(setting);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Chart berhasil diperbarui!";
                return RedirectToAction(nameof(Index));
            }
            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var setting = await _context.ChartSettings.FindAsync(id);
            if (setting != null)
            {
                _context.ChartSettings.Remove(setting);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Chart berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAktif(int id)
        {
            var setting = await _context.ChartSettings.FindAsync(id);
            if (setting != null)
            {
                setting.Aktif = !setting.Aktif;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Chart '{setting.NamaChart}' {(setting.Aktif ? "diaktifkan" : "dinonaktifkan")}!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
