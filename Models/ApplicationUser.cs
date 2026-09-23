using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace itam.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Display(Name = "Nama Lengkap")]
        [StringLength(150)]
        public string? NamaLengkap { get; set; }
    }
}
