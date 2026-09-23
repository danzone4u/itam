namespace itam.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string subject, string htmlMessage, bool isBarangMasuk = false, bool isBarangKeluar = false, bool isPeminjaman = false, bool isPermintaan = false);
        Task<(bool Success, string Message)> TestEmailAsync(string targetEmail);
    }
}
