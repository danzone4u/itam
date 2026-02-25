using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MyGudang.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");
                
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Username dan Password wajib diisi.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(username, password, isPersistent: true, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl ?? "/");
            }

            ViewBag.Error = "Username atau Password salah.";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Profile(string email, string? currentPassword, string? newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            user.Email = email;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                TempData["Error"] = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return View(user);
            }

            if (!string.IsNullOrWhiteSpace(currentPassword) && !string.IsNullOrWhiteSpace(newPassword))
            {
                var passResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                if (!passResult.Succeeded)
                {
                    TempData["Error"] = string.Join(", ", passResult.Errors.Select(e => e.Description));
                    return View(user);
                }
            }

            TempData["Success"] = "Profil berhasil diperbarui!";
            return View(user);
        }
    }
}
