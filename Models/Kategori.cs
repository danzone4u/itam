using System.ComponentModel.DataAnnotations;

namespace MyGudang.Models
{
    public class Kategori
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama Kategori wajib diisi")]
        [Display(Name = "Nama Kategori")]
        [StringLength(100)]
        public string NamaKategori { get; set; } = string.Empty;

        [Display(Name = "Kode Prefix")]
        [StringLength(10)]
        public string? KodePrefix { get; set; }

        [Display(Name = "Deskripsi")]
        [StringLength(500)]
        public string? Deskripsi { get; set; }

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Barang>? Barangs { get; set; }
    }
}
