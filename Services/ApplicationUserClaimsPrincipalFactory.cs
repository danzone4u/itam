using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using itam.Models;

namespace itam.Services
{
    public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public ApplicationUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            if (!string.IsNullOrEmpty(user.NamaLengkap))
            {
                identity.AddClaim(new Claim("NamaLengkap", user.NamaLengkap));
            }
            else if (!string.IsNullOrEmpty(user.UserName))
            {
                identity.AddClaim(new Claim("NamaLengkap", user.UserName));
            }
            return identity;
        }
    }
}
