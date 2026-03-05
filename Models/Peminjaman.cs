using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itam.Models
{
    public class Peminjaman
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Barang")]
        public int BarangId { get; set; }

        [ForeignKey("BarangId")]
        public Barang? Barang { get; set; }

        [Required]
        [Display(Name = "Jumlah")]
        public int Jumlah { get; set; }

        [Required]
        [Display(Name = "Peminjam")]
        [StringLength(200)]
        public string Peminjam { get; set; } = string.Empty;

        [Display(Name = "NIP/NIK")]
        [StringLength(50)]
        public string? NipNik { get; set; }

        [Display(Name = "Departemen")]
        [StringLength(100)]
        public string? Departemen { get; set; }

        [Display(Name = "No. HP")]
        [StringLength(20)]
        public string? NoHp { get; set; }

        [Required]
        [Display(Name = "Tanggal Pinjam")]
        public DateTime TanggalPinjam { get; set; }

        [Required]
        [Display(Name = "Tanggal Jatuh Tempo")]
        public DateTime TanggalJatuhTempo { get; set; }

        [Display(Name = "Tanggal Kembali")]
        public DateTime? TanggalKembali { get; set; }

        [Required]
        [Display(Name = "Status")]
        [StringLength(20)]
        public string Status { get; set; } = "Dipinjam"; // Dipinjam, Dikembalikan, Terlambat

        [Display(Name = "Kondisi Kembali")]
        [StringLength(50)]
        public string? KondisiKembali { get; set; }

        [Display(Name = "No. Peminjaman")]
        [StringLength(100)]
        public string? NoPeminjaman { get; set; }

        [Display(Name = "Keterangan")]
        [StringLength(500)]
        public string? Keterangan { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
