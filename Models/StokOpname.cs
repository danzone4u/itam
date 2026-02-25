using System.ComponentModel.DataAnnotations;

namespace MyGudang.Models
{
    public class StokOpname
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tanggal Opname wajib diisi")]
        [Display(Name = "Tanggal Opname")]
        [DataType(DataType.Date)]
        public DateTime TanggalOpname { get; set; } = DateTime.Now;

        [Display(Name = "Keterangan")]
        [StringLength(500)]
        public string? Keterangan { get; set; }

        [Display(Name = "Status")]
        [StringLength(20)]
        public string Status { get; set; } = "Draft"; // Draft or Final

        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<StokOpnameDetail>? Details { get; set; }
    }

    public class StokOpnameDetail
    {
        public int Id { get; set; }

        [Required]
        public int StokOpnameId { get; set; }
        public StokOpname? StokOpname { get; set; }

        [Required]
        [Display(Name = "Barang")]
        public int BarangId { get; set; }
        public Barang? Barang { get; set; }

        [Display(Name = "Stok Sistem")]
        public int StokSistem { get; set; }

        [Display(Name = "Stok Fisik")]
        public int StokFisik { get; set; }

        [Display(Name = "Selisih")]
        public int Selisih { get; set; }

        [Display(Name = "Keterangan")]
        [StringLength(500)]
        public string? Keterangan { get; set; }
    }
}
