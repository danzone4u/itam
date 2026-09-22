using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using itam.Data;
using itam.Models;
using Microsoft.EntityFrameworkCore;

namespace itam.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Settings/Notification
        public async Task<IActionResult> Notification()
        {
            var telegram = await _context.TelegramBotSettings.FirstOrDefaultAsync() ?? new TelegramBotSetting();
            var email = await _context.EmailSettings.FirstOrDefaultAsync() ?? new EmailSetting();

            var vm = new NotificationSettingsViewModel
            {
                TelegramEnabled = telegram.IsEnabled,
                TelegramBotToken = telegram.BotToken,
                TelegramChatIds = telegram.ChatIds,
                
                EmailEnabled = email.IsEnabled,
                SmtpServer = email.SmtpServer,
                SmtpPort = email.SmtpPort,
                UseSsl = email.UseSsl,
                SenderName = email.SenderName,
                SenderEmail = email.SenderEmail,
                SenderPassword = email.SenderPassword,
                ToEmails = email.ToEmails,
                CcEmails = email.CcEmails,
                
                NotifBarangMasuk = email.NotifBarangMasuk,
                NotifBarangKeluar = email.NotifBarangKeluar,
                NotifPeminjaman = email.NotifPeminjaman,
                EmailHtmlTemplate = email.EmailHtmlTemplate
            };
            return View(vm);
        }

        // POST: /Settings/SaveNotification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNotification(NotificationSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Notification", model);
            }

            var telegram = await _context.TelegramBotSettings.FirstOrDefaultAsync();
            if (telegram == null)
            {
                telegram = new TelegramBotSetting();
                _context.TelegramBotSettings.Add(telegram);
            }
            telegram.IsEnabled = model.TelegramEnabled;
            telegram.BotToken = model.TelegramBotToken ?? string.Empty;
            telegram.ChatIds = model.TelegramChatIds ?? string.Empty;

            var email = await _context.EmailSettings.FirstOrDefaultAsync();
            if (email == null)
            {
                email = new EmailSetting();
                _context.EmailSettings.Add(email);
            }
            email.IsEnabled = model.EmailEnabled;
            email.SmtpServer = model.SmtpServer ?? string.Empty;
            email.SmtpPort = model.SmtpPort ?? 587;
            email.UseSsl = model.UseSsl;
            email.SenderName = model.SenderName ?? string.Empty;
            email.SenderEmail = model.SenderEmail ?? string.Empty;
            email.SenderPassword = model.SenderPassword ?? string.Empty;
            email.ToEmails = model.ToEmails ?? string.Empty;
            email.CcEmails = model.CcEmails ?? string.Empty;
            
            email.NotifBarangMasuk = model.NotifBarangMasuk;
            email.NotifBarangKeluar = model.NotifBarangKeluar;
            email.NotifPeminjaman = model.NotifPeminjaman;

            // Jangan biarkan template email kosong, jika dikosongkan secara tidak sengaja,
            // kembalikan ke default bawaan dari model EmailSetting
            if (!string.IsNullOrWhiteSpace(model.EmailHtmlTemplate))
            {
                email.EmailHtmlTemplate = model.EmailHtmlTemplate;
            }
            else
            {
                // Assign new instance to reset to default
                email.EmailHtmlTemplate = new EmailSetting().EmailHtmlTemplate;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Pengaturan notifikasi berhasil disimpan.";
            return RedirectToAction(nameof(Notification));
        }
    }
}
