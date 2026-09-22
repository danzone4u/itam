using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using itam.Data;
using itam.Models;
using itam.Services;
using System.Linq;

namespace itam.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class TelegramBotSettingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITelegramService _telegram;

        public TelegramBotSettingController(ApplicationDbContext context, ITelegramService telegram)
        {
            _context = context;
            _telegram = telegram;
        }

        public IActionResult Index()
        {
            var setting = _context.TelegramBotSettings.FirstOrDefault();
            if (setting == null)
            {
                setting = new TelegramBotSetting();
            }
            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(TelegramBotSetting model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var existing = _context.TelegramBotSettings.FirstOrDefault();
            if (existing == null)
            {
                _context.TelegramBotSettings.Add(model);
            }
            else
            {
                existing.BotToken = model.BotToken;
                existing.ChatIds = model.ChatIds;
                existing.IsEnabled = model.IsEnabled;
                _context.TelegramBotSettings.Update(existing);
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Pengaturan Telegram Bot berhasil disimpan.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestConnection()
        {
            var setting = _context.TelegramBotSettings.FirstOrDefault();
            if (setting == null || string.IsNullOrWhiteSpace(setting.BotToken))
            {
                TempData["Error"] = "Simpan token bot terlebih dahulu sebelum melakukan test.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Send test message
                var testMsg = "🤖 *IT Asset Management Test Notifikasi*\n\nKoneksi bot Telegram berhasil terhubung dengan aplikasi IT-AM!";
                await _telegram.SendAsync(testMsg);
                TempData["Success"] = "Pesan tes berhasil dikirim ke Telegram!";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"Gagal mengirim pesan tes: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
