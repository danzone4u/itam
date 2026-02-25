using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyGudang.Models
{
    public class BarangMasuk
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Barang wajib dipilih")]
        [Display(Name = "Barang")]
        public int BarangId { get; set; }

        [ForeignKey("BarangId")]
        public Barang? Barang { get; set; }

        [Required(ErrorMessage = "Jumlah wajib diisi")]
        [Display(Name = "Jumlah")]
        [Range(1, int.MaxValue, ErrorMessage = "Jumlah harus lebih dari 0")]
        public int Jumlah { get; set; }

        [Required(ErrorMessage = "Tanggal Masuk wajib diisi")]
        [Display(Name = "Tanggal Masuk")]
        [DataType(DataType.Date)]
        public DateTime TanggalMasuk { get; set; } = DateTime.Now;

        [Display(Name = "Keterangan")]
        [StringLength(500)]
        public string? Keterangan { get; set; }

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ruangan")]
        public int? LokasiId { get; set; }

        [ForeignKey("LokasiId")]
        public Lokasi? Lokasi { get; set; }
    }
}
