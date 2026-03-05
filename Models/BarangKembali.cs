using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itam.Models
{
    public class BarangKembali
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Barang wajib dipilih")]
        [Display(Name = "Barang")]
        public int BarangId { get; set; }

        [ForeignKey("BarangId")]
        public Barang? Barang { get; set; }

        [Display(Name = "Referensi Barang Keluar")]
        public int? BarangKeluarId { get; set; }

        [ForeignKey("BarangKeluarId")]
        public BarangKeluar? BarangKeluar { get; set; }

        [Required(ErrorMessage = "Jumlah wajib diisi")]
        [Display(Name = "Jumlah")]
        [Range(1, int.MaxValue, ErrorMessage = "Jumlah harus lebih dari 0")]
        public int Jumlah { get; set; }

        [Required(ErrorMessage = "Tanggal Kembali wajib diisi")]
        [Display(Name = "Tanggal Kembali")]
        [DataType(DataType.Date)]
        public DateTime TanggalKembali { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Kondisi wajib dipilih")]
        [Display(Name = "Kondisi")]
        [StringLength(50)]
        public string Kondisi { get; set; } = "Baik";

        [Required(ErrorMessage = "Dikembalikan oleh wajib diisi")]
        [Display(Name = "Dikembalikan Oleh")]
        [StringLength(200)]
        public string DikembalikanOleh { get; set; } = string.Empty;

        [Display(Name = "Keterangan")]
        [StringLength(500)]
        public string? Keterangan { get; set; }

        [Required(ErrorMessage = "Tindak lanjut wajib dipilih")]
        [Display(Name = "Tindak Lanjut")]
        [StringLength(50)]
        public string TindakLanjut { get; set; } = "Dikembalikan ke Stok";

        public ICollection<BarangSerial> BarangSerials { get; set; } = new List<BarangSerial>();

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
