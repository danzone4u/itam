using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyGudang.Models
{
    public class Barang
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kode Barang wajib diisi")]
        [Display(Name = "Kode Barang")]
        [StringLength(50)]
        public string KodeBarang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nama Barang wajib diisi")]
        [Display(Name = "Nama Barang")]
        [StringLength(200)]
        public string NamaBarang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategori wajib dipilih")]
        [Display(Name = "Kategori")]
        public int KategoriId { get; set; }

        [ForeignKey("KategoriId")]
        public Kategori? Kategori { get; set; }

        [Required(ErrorMessage = "Supplier wajib dipilih")]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }

        [Required(ErrorMessage = "Satuan wajib diisi")]
        [Display(Name = "Satuan")]
        [StringLength(50)]
        public string Satuan { get; set; } = string.Empty;

        [Display(Name = "Stok")]
        public int Stok { get; set; } = 0;

        [Display(Name = "Gambar")]
        [StringLength(500)]
        public string? Gambar { get; set; }

        [Display(Name = "Deskripsi")]
        [StringLength(1000)]
        public string? Deskripsi { get; set; }

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<BarangMasuk>? BarangMasuks { get; set; }
        public ICollection<BarangKeluar>? BarangKeluars { get; set; }
        public ICollection<BarangKembali>? BarangKembalis { get; set; }
        public ICollection<BarangSerial>? BarangSerials { get; set; }
    }
}
