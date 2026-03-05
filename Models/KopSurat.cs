using System.ComponentModel.DataAnnotations;

namespace MyGudang.Models
{
    public class KopSurat
    {
        public int Id { get; set; }

        [Display(Name = "Nama Perusahaan")]
        [StringLength(200)]
        public string NamaPerusahaan { get; set; } = "Pertamina Patra Niaga";

        [Display(Name = "Sub Judul / Divisi")]
        [StringLength(200)]
        public string? SubJudul { get; set; } = "Sistem Inventori Perangkat";

        [Display(Name = "Alamat")]
        [StringLength(500)]
        public string? Alamat { get; set; }

        [Display(Name = "Telepon")]
        [StringLength(50)]
        public string? Telepon { get; set; }

        [Display(Name = "Email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Display(Name = "Website")]
        [StringLength(200)]
        public string? Website { get; set; }

        [Display(Name = "Nama Penandatangan Pengirim")]
        [StringLength(200)]
        public string? NamaPengirim { get; set; }

        [Display(Name = "Jabatan Pengirim")]
        [StringLength(200)]
        public string? JabatanPengirim { get; set; }

        [Display(Name = "Tampilkan Logo")]
        public bool TampilkanLogo { get; set; } = true;

        [StringLength(255)]
        public string? LogoPath { get; set; }
    }
}
