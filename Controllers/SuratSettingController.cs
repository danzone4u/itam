using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;

namespace MyGudang.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuratSettingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuratSettingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var setting = await _context.SuratSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new SuratSetting();
                _context.SuratSettings.Add(setting);
                await _context.SaveChangesAsync();
            }

            // Generate preview
            var count = await _context.BarangKeluars.CountAsync() + 1;
            ViewBag.Preview = GenerateNomorSurat(setting, count, "SJ");
            ViewBag.PreviewKembali = GenerateNomorSurat(setting, 1, "SK");
            ViewBag.PreviewTerima = GenerateNomorSurat(setting, 1, "STB");
            ViewBag.PreviewPeminjaman = GenerateNomorSurat(setting, 1, "SP");

            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SuratSetting model)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.SuratSettings.FirstOrDefaultAsync();
                if (existing != null)
                {
                    existing.PrefixSuratJalan = model.PrefixSuratJalan;
                    existing.PrefixSuratKembali = model.PrefixSuratKembali;
                    existing.PrefixSuratTerima = model.PrefixSuratTerima;
                    existing.PrefixSuratPeminjaman = model.PrefixSuratPeminjaman;
                    existing.FormatTanggal = model.FormatTanggal;
                    existing.PanjangNomorUrut = model.PanjangNomorUrut;
                    existing.Separator = model.Separator;
                    existing.Suffix = model.Suffix;
                    existing.ResetBulanan = model.ResetBulanan;
                }
                else
                {
                    _context.SuratSettings.Add(model);
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Setting nomor surat berhasil disimpan!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public static string GenerateNomorSurat(SuratSetting? setting, int counter, string type)
        {
            if (setting == null)
                setting = new SuratSetting();

            var prefix = type == "SK" ? setting.PrefixSuratKembali 
                       : type == "STB" ? setting.PrefixSuratTerima 
                       : type == "SP" ? setting.PrefixSuratPeminjaman
                       : setting.PrefixSuratJalan;
            var sep = setting.Separator ?? "-";
            var tanggal = DateTime.Now.ToString(setting.FormatTanggal ?? "yyyyMMdd");
            var nomor = counter.ToString($"D{setting.PanjangNomorUrut}");

            var result = string.IsNullOrWhiteSpace(setting.Suffix)
                ? $"{prefix}{sep}{tanggal}{sep}{nomor}"
                : $"{prefix}{sep}{setting.Suffix}{sep}{tanggal}{sep}{nomor}";

            return result;
        }
    }
}
