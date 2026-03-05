using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itam.Models
{
    public class TransferBarang
    {
        public int Id { get; set; }

        [Required]
        public int BarangId { get; set; }

        [ForeignKey("BarangId")]
        public Barang? Barang { get; set; }

        [Required]
        [Display(Name = "Dari Lokasi")]
        public int DariLokasiId { get; set; }

        [ForeignKey("DariLokasiId")]
        public Lokasi? DariLokasi { get; set; }

        [Required]
        [Display(Name = "Ke Lokasi")]
        public int KeLokasiId { get; set; }

        [ForeignKey("KeLokasiId")]
        public Lokasi? KeLokasi { get; set; }

        [Required]
        [Display(Name = "Jumlah")]
        public int Jumlah { get; set; }

        [Required]
        [Display(Name = "Tanggal Transfer")]
        public DateTime TanggalTransfer { get; set; }

        [Display(Name = "Keterangan")]
        [StringLength(500)]
        public string? Keterangan { get; set; }

        [Display(Name = "No. Transfer")]
        [StringLength(100)]
        public string? NoTransfer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<TransferBarangSerial>? TransferBarangSerials { get; set; }
    }
}
