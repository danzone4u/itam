using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace itam.Models
{
    public class BarangKeluar
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

        [Required(ErrorMessage = "Tanggal Keluar wajib diisi")]
        [Display(Name = "Tanggal Keluar")]
        [DataType(DataType.Date)]
        public DateTime TanggalKeluar { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Penerima wajib diisi")]
        [Display(Name = "Penerima")]
        [StringLength(200)]
        public string Penerima { get; set; } = string.Empty;

        [Display(Name = "Alamat Penerima")]
        [StringLength(500)]
        public string? Alamat { get; set; }

        [Display(Name = "No. HP Penerima")]
        [StringLength(20)]
        public string? NoHpPenerima { get; set; }

        [Display(Name = "Keterangan")]
        [StringLength(500)]
        public string? Keterangan { get; set; }

        [Display(Name = "PIC / Pembawa Barang")]
        [StringLength(100)]
        public string? Pic { get; set; }

        [Display(Name = "No. Surat Jalan")]
        [StringLength(100)]
        public string? NoSuratJalan { get; set; }

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ruangan")]
        public int? LokasiId { get; set; }

        [ForeignKey("LokasiId")]
        public Lokasi? Lokasi { get; set; }

        public ICollection<BarangSerial> BarangSerials { get; set; } = new List<BarangSerial>();
    }
}
