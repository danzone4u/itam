using System.ComponentModel.DataAnnotations;

namespace MyGudang.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama Supplier wajib diisi")]
        [Display(Name = "Nama Supplier")]
        [StringLength(200)]
        public string NamaSupplier { get; set; } = string.Empty;

        [Display(Name = "Alamat")]
        [StringLength(500)]
        public string? Alamat { get; set; }

        [Display(Name = "Telepon")]
        [StringLength(20)]
        public string? Telepon { get; set; }

        [Display(Name = "Email")]
        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Barang>? Barangs { get; set; }
    }
}
