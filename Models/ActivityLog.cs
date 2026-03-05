using System.ComponentModel.DataAnnotations;

namespace itam.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        [Display(Name = "User")]
        [StringLength(200)]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Aksi")]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [Display(Name = "Modul")]
        [StringLength(100)]
        public string Module { get; set; } = string.Empty;

        [Display(Name = "Detail")]
        [StringLength(1000)]
        public string? Detail { get; set; }

        [Display(Name = "Waktu")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "IP Address")]
        [StringLength(50)]
        public string? IpAddress { get; set; }
    }
}
