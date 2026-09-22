using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itam.Models
{
    public class Permintaan
    {
        public int Id { get; set; }

        [Display(Name = "No. Permintaan")]
        [StringLength(100)]
        public string? NoPermintaan { get; set; }

        [Required(ErrorMessage = "Jenis Permintaan wajib dipilih")]
        [Display(Name = "Jenis Permintaan")]
        [StringLength(50)]
        public string JenisPermintaan { get; set; } = "BarangKeluar"; // BarangKeluar, Peminjaman

        [Required]
        [Display(Name = "Pemohon")]
        [StringLength(200)]
        public string PemohonUser { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nama Penerima / Pengguna wajib diisi")]
        [Display(Name = "Penerima / Pengguna")]
        [StringLength(200)]
        public string Penerima { get; set; } = string.Empty;

        [Display(Name = "Nopeg / NIP / NIK")]
        [StringLength(50)]
        public string? NipNik { get; set; }

        [Display(Name = "Departemen / Unit Kerja")]
        [StringLength(100)]
        public string? Departemen { get; set; }

        [Display(Name = "No. HP")]
        [StringLength(20)]
        public string? NoHp { get; set; }

        [Display(Name = "Lokasi / Ruangan Tujuan")]
        public int? LokasiId { get; set; }

        [ForeignKey("LokasiId")]
        public Lokasi? Lokasi { get; set; }

        [Required(ErrorMessage = "Keperluan wajib diisi")]
        [Display(Name = "Keperluan / Alasan")]
        [StringLength(500)]
        public string Keperluan { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Tanggal Pengajuan")]
        [DataType(DataType.Date)]
        public DateTime TanggalPengajuan { get; set; } = DateTime.Now;

        [Display(Name = "Tanggal Dibutuhkan / Jatuh Tempo")]
        [DataType(DataType.Date)]
        public DateTime? TanggalDibutuhkan { get; set; }

        [Required]
        [Display(Name = "Status")]
        [StringLength(50)]
        public string Status { get; set; } = "Menunggu Approval"; // Menunggu Approval, Disetujui, Ditolak, Dibatalkan

        [Display(Name = "Disetujui / Ditolak Oleh")]
        [StringLength(200)]
        public string? ApprovedBy { get; set; }

        [Display(Name = "Waktu Approval")]
        public DateTime? ApprovedAt { get; set; }

        [Display(Name = "Catatan Admin")]
        [StringLength(500)]
        public string? CatatanAdmin { get; set; }

        public int? BarangKeluarId { get; set; }

        [ForeignKey("BarangKeluarId")]
        public BarangKeluar? BarangKeluar { get; set; }

        public int? PeminjamanId { get; set; }

        [ForeignKey("PeminjamanId")]
        public Peminjaman? Peminjaman { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PermintaanDetail> Details { get; set; } = new List<PermintaanDetail>();
    }
}
