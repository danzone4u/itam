using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using itam.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;

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

        private static (string Username, string Password) GetCredentials(string senderEmail, string rawPassword)
        {
            var cleanPassword = (rawPassword ?? "").Replace(" ", "").Trim();
            var username = (senderEmail ?? "").Trim();

            if (username.Contains('@'))
            {
                username = username.Split('@', 2)[0];
            }
            else if (username.Contains('\\'))
            {
                username = username.Split('\\', 2)[1];
            }

            return (username, cleanPassword);
        }

        public async Task SendEmailAsync(string subject, string htmlMessage, bool isBarangMasuk = false, bool isBarangKeluar = false, bool isPeminjaman = false, bool isPermintaan = false)
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
                if (isPermintaan && !setting.NotifPermintaan) return;

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
                string themeColor = isBarangMasuk ? "#28a745" : isBarangKeluar ? "#dc3545" : isPeminjaman ? "#6f42c1" : isPermintaan ? "#fd7e14" : "#007bff";
                
                string template = setting.EmailHtmlTemplate;
                if (string.IsNullOrWhiteSpace(template))
                {
                    template = new Models.EmailSetting().EmailHtmlTemplate;
                }

                string finalHtml = template
                    .Replace("{themeColor}", themeColor)
                    .Replace("{Subject}", subject)
                    .Replace("{Subjek}", subject)
                    .Replace("{Content}", formattedText)
                    .Replace("{Date}", DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss"));

                var bodyBuilder = new BodyBuilder { HtmlBody = finalHtml };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                var secureOption = setting.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(setting.SmtpServer, setting.SmtpPort, secureOption);

                if (!string.IsNullOrWhiteSpace(setting.SenderPassword))
                {
                    var (user, pass) = GetCredentials(setting.SenderEmail, setting.SenderPassword);
                    try
                    {
                        await client.AuthenticateAsync(user, pass);
                    }
                    catch
                    {
                        // Fallback to full email if samAccountName fails
                        await client.AuthenticateAsync(setting.SenderEmail.Trim(), pass);
                    }
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("EmailService: Email berhasil dikirim ke {Count} penerima.", toList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailService: Gagal mengirim email: {Subject}", subject);
            }
        }

        public async Task<(bool Success, string Message)> TestEmailAsync(string targetEmail)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var setting = await context.EmailSettings.OrderBy(e => e.Id).FirstOrDefaultAsync();

                if (setting == null)
                    return (false, "Pengaturan email belum dikonfigurasi di database.");

                if (string.IsNullOrWhiteSpace(setting.SmtpServer) || string.IsNullOrWhiteSpace(setting.SenderEmail))
                    return (false, "SMTP Server atau Email Pengirim belum diisi.");

                if (string.IsNullOrWhiteSpace(targetEmail))
                    return (false, "Email tujuan pengujian belum ditentukan.");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(setting.SenderName, setting.SenderEmail));
                message.To.Add(MailboxAddress.Parse(targetEmail.Trim()));
                message.Subject = "Uji Coba Notifikasi Email - IT Asset Management";

                string testContent = "Halo!<br><br>Ini adalah email uji coba dari sistem <strong>IT Asset Management (MyGudang)</strong>. Jika Anda menerima email ini, berarti konfigurasi SMTP sudah berfungsi dengan baik.<br><br>Waktu Pengiriman: " + DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss");

                string template = setting.EmailHtmlTemplate;
                if (string.IsNullOrWhiteSpace(template))
                {
                    template = new Models.EmailSetting().EmailHtmlTemplate;
                }

                string finalHtml = template
                    .Replace("{themeColor}", "#007bff")
                    .Replace("{Subject}", "Uji Coba Notifikasi Email")
                    .Replace("{Subjek}", "Uji Coba Notifikasi Email")
                    .Replace("{Content}", testContent)
                    .Replace("{Date}", DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss"));

                var bodyBuilder = new BodyBuilder { HtmlBody = finalHtml };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                var secureOption = setting.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                
                await client.ConnectAsync(setting.SmtpServer, setting.SmtpPort, secureOption);

                if (!string.IsNullOrWhiteSpace(setting.SenderPassword))
                {
                    var (user, pass) = GetCredentials(setting.SenderEmail, setting.SenderPassword);
                    try
                    {
                        await client.AuthenticateAsync(user, pass);
                    }
                    catch
                    {
                        await client.AuthenticateAsync(setting.SenderEmail.Trim(), pass);
                    }
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return (true, $"Email uji coba berhasil dikirim ke {targetEmail}.");
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException != null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
                _logger.LogError(ex, "EmailService: Gagal saat menguji kirim email.");
                return (false, $"Gagal mengirim email: {detail}");
            }
        }
    }
}
