using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyGudang.Models
{
    public class BarangSerial
    {
        public int Id { get; set; }

        [Required]
        public int BarangId { get; set; }

        [ForeignKey("BarangId")]
        public Barang? Barang { get; set; }

        [Required]
        [Display(Name = "Serial Number")]
        [StringLength(200)]
        public string SerialNumber { get; set; } = "-";

        [Required]
        [Display(Name = "Status")]
        [StringLength(20)]
        public string Status { get; set; } = "Tersedia"; // Tersedia, Keluar

        public int? BarangMasukId { get; set; }

        [ForeignKey("BarangMasukId")]
        public BarangMasuk? BarangMasuk { get; set; }

        public int? BarangKeluarId { get; set; }

        [ForeignKey("BarangKeluarId")]
        public BarangKeluar? BarangKeluar { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
