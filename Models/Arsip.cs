using System.ComponentModel.DataAnnotations;

namespace MyGudang.Models
{
    public class Arsip
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama Dokumen wajib diisi")]
        [Display(Name = "Nama Dokumen")]
        [StringLength(200)]
        public string NamaDokumen { get; set; } = string.Empty;

        [Display(Name = "Jenis Dokumen")]
        [StringLength(100)]
        public string? JenisDokumen { get; set; }

        [Display(Name = "File")]
        [StringLength(500)]
        public string? FilePath { get; set; }

        [Display(Name = "Keterangan")]
        [StringLength(500)]
        public string? Keterangan { get; set; }

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
