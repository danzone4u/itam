using System.ComponentModel.DataAnnotations;

namespace itam.Models
{
    public class ChartSetting
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama Chart wajib diisi")]
        [Display(Name = "Nama Chart")]
        [StringLength(100)]
        public string NamaChart { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Tipe Chart")]
        [StringLength(30)]
        public string TipeChart { get; set; } = "bar"; // bar, line, doughnut, pie, polarArea, radar

        [Display(Name = "Sumber Data")]
        [StringLength(50)]
        public string SumberData { get; set; } = "masuk_keluar"; // masuk_keluar, per_kategori, stok_barang

        [Display(Name = "Jumlah Bulan")]
        public int JumlahBulan { get; set; } = 6;

        [Display(Name = "Warna Utama")]
        [StringLength(20)]
        public string? WarnaUtama { get; set; } = "#007bff";

        [Display(Name = "Warna Kedua")]
        [StringLength(20)]
        public string? WarnaKedua { get; set; } = "#dc3545";

        [Display(Name = "Tampilkan di Dashboard")]
        public bool Aktif { get; set; } = true;

        [Display(Name = "Urutan")]
        public int Urutan { get; set; } = 0;

        [Display(Name = "Lebar (col-md)")]
        public int Lebar { get; set; } = 6; // 4, 6, 8, 12

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
