using System.ComponentModel.DataAnnotations;

namespace itam.Models
{
    public class EmailSetting
    {
        public int Id { get; set; }

        [Display(Name = "SMTP Server")]
        [StringLength(200)]
        public string SmtpServer { get; set; } = "smtp.gmail.com";

        [Display(Name = "SMTP Port")]
        public int SmtpPort { get; set; } = 587;

        [Display(Name = "Gunakan SSL/TLS")]
        public bool UseSsl { get; set; } = true;

        [Display(Name = "Nama Pengirim")]
        [StringLength(200)]
        public string SenderName { get; set; } = "IT Asset Management";

        [Display(Name = "Email Pengirim")]
        [StringLength(200)]
        [EmailAddress]
        public string SenderEmail { get; set; } = string.Empty;

        [Display(Name = "Password / App Password")]
        [StringLength(300)]
        public string SenderPassword { get; set; } = string.Empty;

        [Display(Name = "Email Penerima (To)")]
        [StringLength(2000)]
        public string ToEmails { get; set; } = string.Empty;

        [Display(Name = "Email CC (Tembusan)")]
        [StringLength(2000)]
        public string? CcEmails { get; set; }

        [Display(Name = "Aktifkan Notifikasi Email")]
        public bool IsEnabled { get; set; } = false;

        [Display(Name = "Notifikasi Barang Masuk")]
        public bool NotifBarangMasuk { get; set; } = true;

        [Display(Name = "Notifikasi Barang Keluar")]
        public bool NotifBarangKeluar { get; set; } = true;

        [Display(Name = "Notifikasi Peminjaman")]
        public bool NotifPeminjaman { get; set; } = true;

        [Display(Name = "Notifikasi Permintaan Barang")]
        public bool NotifPermintaan { get; set; } = true;

        [Display(Name = "Template HTML Email")]
        public string EmailHtmlTemplate { get; set; } = @"<!DOCTYPE html>
<html>
<head>
<style>
    body { margin: 0; padding: 30px; background-color: #d6d6d6; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
    .paper { max-width: 700px; margin: 0 auto; background: #ffffff; padding: 50px 60px; box-shadow: 0 2px 12px rgba(0,0,0,0.15); }
    .company-info h2 { margin: 0; font-size: 20px; color: #1a1a1a; font-weight: 700; letter-spacing: 0.5px; }
    .company-info p { margin: 3px 0; font-size: 13px; color: #555; }
    .company-info .address { font-size: 12px; color: #777; }
    .header-line { border: none; border-top: 3px solid {themeColor}; margin: 15px 0 30px 0; }
    .doc-title { text-align: center; font-size: 18px; font-weight: 700; color: #1a1a1a; margin: 10px 0 30px 0; letter-spacing: 1.5px; text-transform: uppercase; text-decoration: underline; text-underline-offset: 6px; }
    .field-table { width: auto; margin-bottom: 20px; border-collapse: collapse; }
    .field-table td { padding: 5px 0; font-size: 14px; color: #333; vertical-align: top; }
    .field-label { width: 170px; font-weight: 600; }
    .field-sep { width: 15px; text-align: center; }
    .intro-text { font-size: 14px; color: #333; margin: 20px 0 10px 0; }
    .items-table { width: 100%; border-collapse: collapse; margin: 15px 0 20px 0; font-size: 13px; }
    .items-table th { background-color: #f0f0f0; border: 1px solid #bbb; padding: 10px 12px; text-align: left; font-weight: 600; color: #333; }
    .items-table td { border: 1px solid #bbb; padding: 8px 12px; color: #444; }
    .items-table .col-no { text-align: center; width: 40px; }
    .items-table .col-jumlah { text-align: center; width: 65px; }
    .items-table .col-satuan { text-align: center; width: 65px; }
    .note { margin-top: 25px; padding: 15px; background-color: #f9f9f9; border-left: 4px solid {themeColor}; font-size: 13px; color: #555; font-style: italic; line-height: 1.7; }
    .footer { margin-top: 40px; padding-top: 15px; border-top: 1px solid #ddd; text-align: center; font-size: 11px; color: #aaa; line-height: 1.8; }
</style>
</head>
<body>
<div class='paper'>
    <div class='company-info'>
        <h2>PT PERTAMINA PATRA NIAGA</h2>
        <p>IT Region Jatimbalinus</p>
        <p class='address'>Jl. Jagir Wonokromo No. 88 - Surabaya</p>
    </div>
    <hr class='header-line'>
    <div class='doc-title'>{Subject}</div>
    {Content}
    <div class='note'>
        Dokumen ini di-generate secara otomatis oleh sistem <strong>IT Asset Management</strong> pada {Date}.
    </div>
    <div class='footer'>
        &copy; 2026 IT Region Jatimbalinus &mdash; IT Asset Management<br>
        Email ini dikirim secara otomatis oleh sistem, mohon tidak dibalas.
    </div>
</div>
</body>
</html>";

        /// <summary>
        /// Mendapatkan daftar email To yang telah dipecah dari string
        /// </summary>
        public List<string> GetToEmailList()
        {
            if (string.IsNullOrWhiteSpace(ToEmails)) return new List<string>();
            return ToEmails.Split(new[] { ';', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(e => e.Trim())
                           .Where(e => !string.IsNullOrEmpty(e))
                           .ToList();
        }

        /// <summary>
        /// Mendapatkan daftar email CC yang telah dipecah dari string
        /// </summary>
        public List<string> GetCcEmailList()
        {
            if (string.IsNullOrWhiteSpace(CcEmails)) return new List<string>();
            return CcEmails.Split(new[] { ';', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(e => e.Trim())
                           .Where(e => !string.IsNullOrEmpty(e))
                           .ToList();
        }
    }
}
