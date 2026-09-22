using System.ComponentModel.DataAnnotations;

namespace itam.Models
{
    public class NotificationSettingsViewModel
    {
        // ----------------- TELEGRAM SETTINGS -----------------
        [Display(Name = "Aktifkan Notifikasi Telegram")]
        public bool TelegramEnabled { get; set; }

        [Display(Name = "Bot Token")]
        public string? TelegramBotToken { get; set; }

        [Display(Name = "Chat ID(s) (Pisahkan dengan koma)")]
        public string? TelegramChatIds { get; set; }


        // ----------------- EMAIL SETTINGS -----------------
        [Display(Name = "Aktifkan Notifikasi Email")]
        public bool EmailEnabled { get; set; }

        [Display(Name = "SMTP Server")]
        public string? SmtpServer { get; set; }

        [Display(Name = "SMTP Port")]
        public int? SmtpPort { get; set; }

        [Display(Name = "Gunakan SSL/TLS")]
        public bool UseSsl { get; set; }

        [Display(Name = "Nama Pengirim")]
        public string? SenderName { get; set; }

        [Display(Name = "Email Pengirim")]
        [EmailAddress]
        public string? SenderEmail { get; set; }

        [Display(Name = "Password / App Password")]
        public string? SenderPassword { get; set; }

        [Display(Name = "Email Penerima (To)")]
        public string? ToEmails { get; set; }

        [Display(Name = "Email Tembusan (CC)")]
        public string? CcEmails { get; set; }


        // ----------------- PER-EVENT EMAIL TOGGLES -----------------
        [Display(Name = "Barang Masuk")]
        public bool NotifBarangMasuk { get; set; }

        [Display(Name = "Barang Keluar")]
        public bool NotifBarangKeluar { get; set; }

        [Display(Name = "Peminjaman / Pengembalian")]
        public bool NotifPeminjaman { get; set; }

        // ----------------- TEMPLATE EMAIL -----------------
        [Display(Name = "Template HTML Email")]
        public string? EmailHtmlTemplate { get; set; }
    }
}
