using System.ComponentModel.DataAnnotations;

namespace itam.Models
{
    public class TelegramBotSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string BotToken { get; set; } = string.Empty;

        /// <summary>
        /// Chat IDs separated by commas. Each can be a user or group ID (e.g. -1234567890).
        /// </summary>
        [StringLength(500)]
        public string ChatIds { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = false;
    }
}
