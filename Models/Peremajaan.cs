using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyGudang.Models
{
    public class Peremajaan
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Barang wajib dipilih")]
        [Display(Name = "Barang")]
        public int BarangId { get; set; }

        [ForeignKey("BarangId")]
        public Barang? Barang { get; set; }

        [Display(Name = "Ref. Barang Keluar")]
        public int? BarangKeluarId { get; set; }

        [ForeignKey("BarangKeluarId")]
        public BarangKeluar? BarangKeluar { get; set; }

        [Required]
        [Display(Name = "Jumlah")]
        [Range(1, int.MaxValue)]
        public int Jumlah { get; set; }

        [Required]
        [Display(Name = "Tanggal Peremajaan")]
        [DataType(DataType.Date)]
        public DateTime TanggalPeremajaan { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Dikembalikan Oleh")]
        [StringLength(200)]
        public string DikembalikanOleh { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Kondisi Barang")]
        [StringLength(50)]
        public string Kondisi { get; set; } = string.Empty; // Layak Pakai, Perlu Perbaikan, Rusak, Disposal

        [Required]
        [Display(Name = "Tindak Lanjut")]
        [StringLength(50)]
        public string TindakLanjut { get; set; } = string.Empty; // Masuk Inventaris, Disposal, Perbaikan

        [Display(Name = "Keterangan")]
        [StringLength(1000)]
        public string? Keterangan { get; set; }

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
