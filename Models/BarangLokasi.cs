using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itam.Models
{
    public class BarangLokasi
    {
        public int Id { get; set; }

        [Required]
        public int BarangId { get; set; }

        [ForeignKey("BarangId")]
        public Barang? Barang { get; set; }

        [Required]
        public int LokasiId { get; set; }

        [ForeignKey("LokasiId")]
        public Lokasi? Lokasi { get; set; }

        [Required]
        [Display(Name = "Stok")]
        public int Stok { get; set; } = 0;
    }
}
