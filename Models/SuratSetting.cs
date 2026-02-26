using System.ComponentModel.DataAnnotations;

namespace MyGudang.Models
{
    public class SuratSetting
    {
        public int Id { get; set; }

        [Display(Name = "Prefix Surat Jalan")]
        [StringLength(50)]
        public string PrefixSuratJalan { get; set; } = "SJ";

        [Display(Name = "Prefix Surat Pengembalian")]
        [StringLength(50)]
        public string PrefixSuratKembali { get; set; } = "SK";

        [Display(Name = "Prefix Surat Terima Barang")]
        [StringLength(50)]
        public string PrefixSuratTerima { get; set; } = "STB";

        [Display(Name = "Prefix Surat Peminjaman")]
        [StringLength(50)]
        public string PrefixSuratPeminjaman { get; set; } = "SP";

        [Display(Name = "Format Tanggal")]
        [StringLength(20)]
        public string FormatTanggal { get; set; } = "yyyyMMdd";

        [Display(Name = "Panjang Nomor Urut")]
        public int PanjangNomorUrut { get; set; } = 4;

        [Display(Name = "Separator")]
        [StringLength(5)]
        public string Separator { get; set; } = "-";

        [Display(Name = "Suffix / Keterangan")]
        [StringLength(50)]
        public string? Suffix { get; set; }

        [Display(Name = "Reset Nomor Urut Tiap Bulan")]
        public bool ResetBulanan { get; set; } = false;
    }
}
