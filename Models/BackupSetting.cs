using System.ComponentModel.DataAnnotations;

namespace MyGudang.Models
{
    public class BackupSetting
    {
        public int Id { get; set; }

        [Display(Name = "Auto Backup")]
        public bool AutoBackupEnabled { get; set; } = false;

        [Display(Name = "Interval (Jam)")]
        public int IntervalHours { get; set; } = 24;

        [Display(Name = "Backup Terakhir")]
        public DateTime? LastBackupAt { get; set; }

        [Display(Name = "Lokasi Backup")]
        [StringLength(500)]
        public string BackupPath { get; set; } = "wwwroot/backups";
    }
}
