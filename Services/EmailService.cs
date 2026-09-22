using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using itam.Data;
using Microsoft.EntityFrameworkCore;

namespace itam.Services
{
    public class EmailService : IEmailService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IServiceScopeFactory scopeFactory, ILogger<EmailService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task SendEmailAsync(string subject, string htmlMessage, bool isBarangMasuk = false, bool isBarangKeluar = false, bool isPeminjaman = false)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var setting = await context.EmailSettings.OrderBy(e => e.Id).FirstOrDefaultAsync();

                if (setting == null || !setting.IsEnabled)
                    return;

                // Check per-event toggles
                if (isBarangMasuk && !setting.NotifBarangMasuk) return;
                if (isBarangKeluar && !setting.NotifBarangKeluar) return;
                if (isPeminjaman && !setting.NotifPeminjaman) return;

                var toList = setting.GetToEmailList();
                var ccList = setting.GetCcEmailList();

                if (!toList.Any())
                {
                    _logger.LogWarning("EmailService: Tidak ada email penerima yang terdaftar.");
                    return;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(setting.SenderName, setting.SenderEmail));

                foreach (var to in toList)
                    message.To.Add(MailboxAddress.Parse(to));

                foreach (var cc in ccList)
                    message.Cc.Add(MailboxAddress.Parse(cc));

                message.Subject = subject;

                // --- PARSE MARKDOWN TO HTML ---
                var formattedText = htmlMessage
                    .Replace("\r\n", "<br>")
                    .Replace("\n", "<br>");

                var boldRegex = new System.Text.RegularExpressions.Regex(@"\*(.*?)\*");
                formattedText = boldRegex.Replace(formattedText, "<strong>$1</strong>");

                if (formattedText.Contains("<strong>Daftar Barang:</strong><br>"))
                {
                    formattedText = formattedText.Replace("<strong>Daftar Barang:</strong><br>", "<div class='item-list'><strong>Daftar Barang:</strong><br>");
                    formattedText += "</div>";
                }

                // --- BUILD HTML TEMPLATE ---
                string themeColor = isBarangMasuk ? "#28a745" : isBarangKeluar ? "#dc3545" : isPeminjaman ? "#6f42c1" : "#007bff";
                
                // Use template from database, fallback to default if empty
                string template = setting.EmailHtmlTemplate;
                if (string.IsNullOrWhiteSpace(template))
                {
                    template = new Models.EmailSetting().EmailHtmlTemplate;
                }

                // Replace placeholders
                string finalHtml = template
                    .Replace("{themeColor}", themeColor)
                    .Replace("{Content}", formattedText);

                var bodyBuilder = new BodyBuilder { HtmlBody = finalHtml };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                // Bypass SSL certificate validation (common issue in IIS/Server deployments)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                var secureOption = setting.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(setting.SmtpServer, setting.SmtpPort, secureOption);
                await client.AuthenticateAsync(setting.SenderEmail, setting.SenderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("EmailService: Email berhasil dikirim ke {Count} penerima.", toList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailService: Gagal mengirim email: {Subject}", subject);
            }
        }
    }
}
