using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itam.Models
{
    public class PermintaanDetail
    {
        public int Id { get; set; }

        [Required]
        public int PermintaanId { get; set; }

        [ForeignKey("PermintaanId")]
        public Permintaan? Permintaan { get; set; }

        [Required(ErrorMessage = "Barang wajib dipilih")]
        [Display(Name = "Barang")]
        public int BarangId { get; set; }

        [ForeignKey("BarangId")]
        public Barang? Barang { get; set; }

        [Required(ErrorMessage = "Jumlah wajib diisi")]
        [Display(Name = "Jumlah")]
        [Range(1, int.MaxValue, ErrorMessage = "Jumlah harus minimal 1")]
        public int Jumlah { get; set; } = 1;

        [Display(Name = "Catatan Item")]
        [StringLength(200)]
        public string? Catatan { get; set; }
    }
}
