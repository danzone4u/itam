using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace itam.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new Dictionary<string, string>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "-";
            }
            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        public IActionResult Create()
        {
            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string username, string email, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Username dan Password wajib diisi!";
                ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
                return View();
            }

            var user = new IdentityUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = true
            };
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(role))
                    await _userManager.AddToRoleAsync(user, role);
                TempData["Success"] = $"User '{username}' berhasil ditambahkan!";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
            return View();
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var currentRoles = await _userManager.GetRolesAsync(user);
            ViewBag.CurrentRole = currentRoles.FirstOrDefault() ?? "";
            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", ViewBag.CurrentRole);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string username, string email, string? newPassword, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.UserName = username;
            user.Email = email;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                TempData["Error"] = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                ViewBag.CurrentRole = role;
                ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", role);
                return View(user);
            }

            // Update role
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!string.IsNullOrEmpty(role))
                await _userManager.AddToRoleAsync(user, role);

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
                if (!passResult.Succeeded)
                {
                    TempData["Error"] = string.Join(", ", passResult.Errors.Select(e => e.Description));
                    ViewBag.CurrentRole = role;
                    ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name", role);
                    return View(user);
                }
            }

            TempData["Success"] = $"User '{username}' berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                if (user.UserName == "admin")
                {
                    TempData["Error"] = "User admin tidak boleh dihapus!";
                    return RedirectToAction(nameof(Index));
                }
                await _userManager.DeleteAsync(user);
                TempData["Success"] = "User berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
