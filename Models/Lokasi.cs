using System.ComponentModel.DataAnnotations;

namespace itam.Models
{
    public class Lokasi
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Kode Lokasi")]
        [StringLength(20)]
        public string Kode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Nama Lokasi")]
        [StringLength(200)]
        public string NamaLokasi { get; set; } = string.Empty;

        [Display(Name = "Alamat")]
        [StringLength(500)]
        public string? Alamat { get; set; }

        [Display(Name = "Penanggung Jawab")]
        [StringLength(200)]
        public string? PenanggungJawab { get; set; }

        [Display(Name = "No. Telp")]
        [StringLength(20)]
        public string? NoTelp { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<BarangLokasi>? BarangLokasis { get; set; }
    }
}
