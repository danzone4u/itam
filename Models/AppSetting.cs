using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyGudang.Models
{
    public class AppSetting
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama Aplikasi harus diisi")]
        [StringLength(100)]
        public string AppName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? LogoPath { get; set; }

        [StringLength(255)]
        public string? FaviconPath { get; set; }
    }
}
