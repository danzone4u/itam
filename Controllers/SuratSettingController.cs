using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;

namespace itam.Controllers
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
            var setting = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new SuratSetting();
                _context.SuratSettings.Add(setting);
                await _context.SaveChangesAsync();
            }

            // Generate preview per template
            var countSJ = await _context.BarangKeluars.CountAsync() + 1;
            var countSTB = await _context.BarangKeluars.CountAsync() + 1;
            var countSK = await _context.BarangKembalis.CountAsync() + 1;
            var countSP = await _context.Peminjamans.CountAsync() + 1;

            ViewBag.Preview = GenerateNomorSurat(setting, countSJ, "SJ");
            ViewBag.PreviewTerima = GenerateNomorSurat(setting, countSTB, "STB");
            ViewBag.PreviewKembali = GenerateNomorSurat(setting, countSK, "SK");
            ViewBag.PreviewPeminjaman = GenerateNomorSurat(setting, countSP, "SP");

            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SuratSetting model)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.SuratSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
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
                    existing.ResetTahunan = model.ResetTahunan;
                    existing.TemplateSuratJalan = model.TemplateSuratJalan;
                    existing.TemplateSuratTerima = model.TemplateSuratTerima;
                    existing.TemplateSuratKembali = model.TemplateSuratKembali;
                    existing.TemplateSuratPeminjaman = model.TemplateSuratPeminjaman;
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

        private static readonly string[] BulanRomawi = new[]
        {
            "", "I", "II", "III", "IV", "V", "VI",
            "VII", "VIII", "IX", "X", "XI", "XII"
        };

        public static string GenerateNomorSurat(SuratSetting? setting, int counter, string type)
        {
            if (setting == null)
                setting = new SuratSetting();

            // Pilih prefix sesuai jenis surat
            var prefix = type switch
            {
                "SK" => setting.PrefixSuratKembali,
                "STB" => setting.PrefixSuratTerima,
                "SP" => setting.PrefixSuratPeminjaman,
                _ => setting.PrefixSuratJalan
            };

            // Pilih template sesuai jenis surat
            var template = type switch
            {
                "SK" => setting.TemplateSuratKembali,
                "STB" => setting.TemplateSuratTerima,
                "SP" => setting.TemplateSuratPeminjaman,
                _ => setting.TemplateSuratJalan
            };

            // Fallback jika template kosong
            if (string.IsNullOrWhiteSpace(template))
            {
                var sep = setting.Separator ?? "-";
                var tanggal = DateTime.Now.ToString(setting.FormatTanggal ?? "yyyyMMdd");
                var nomor = counter.ToString($"D{setting.PanjangNomorUrut}");
                return string.IsNullOrWhiteSpace(setting.Suffix)
                    ? $"{prefix}{sep}{tanggal}{sep}{nomor}"
                    : $"{prefix}{sep}{setting.Suffix}{sep}{tanggal}{sep}{nomor}";
            }

            // Replace tags in template
            var now = DateTime.Now;
            var result = template
                .Replace("{PREFIX}", prefix ?? "")
                .Replace("{NOMOR}", counter.ToString($"D{setting.PanjangNomorUrut}"))
                .Replace("{TAHUN}", now.ToString("yyyy"))
                .Replace("{BULAN_ROMAWI}", BulanRomawi[now.Month])
                .Replace("{BULAN}", now.ToString("MM"))
                .Replace("{TANGGAL}", now.ToString(setting.FormatTanggal ?? "yyyyMMdd"))
                .Replace("{SUFFIX}", setting.Suffix ?? "");

            // Bersihkan separator ganda jika suffix kosong (misal: // menjadi /)
            // Hanya bersihkan jika ada separator berulang di awal/akhir
            while (result.Contains("//")) result = result.Replace("//", "/");
            while (result.Contains("--")) result = result.Replace("--", "-");
            while (result.Contains("..")) result = result.Replace("..", ".");

            // Trim separator di awal dan akhir
            result = result.Trim('/', '-', '.');

            return result;
        }
    }
}
