using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;

namespace MyGudang.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class BackupController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public BackupController(ApplicationDbContext context, IConfiguration config, IWebHostEnvironment env)
        {
            _context = context;
            _config = config;
            _env = env;
        }

        private async Task<string> GetBackupFolderAsync()
        {
            var setting = await _context.BackupSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var path = setting?.BackupPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Path.Combine(_env.WebRootPath, "backups");
            }

            try
            {
                if (!Path.IsPathRooted(path) && !path.Contains("wwwroot"))
                {
                    // If relative, assume it's relative to web root or app root but best to use exact path
                    path = Path.GetFullPath(path); 
                }
                Directory.CreateDirectory(path);
            }
            catch
            {
                path = Path.Combine(_env.WebRootPath, "backups");
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public async Task<IActionResult> Index()
        {
            var setting = await _context.BackupSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new BackupSetting();
                _context.BackupSettings.Add(setting);
                await _context.SaveChangesAsync();
            }

            var backupFolder = await GetBackupFolderAsync();
            var files = Directory.GetFiles(backupFolder, "*.bak")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            ViewBag.Setting = setting;
            ViewBag.BackupFiles = files;
            ViewBag.CurrentPath = backupFolder;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBackup()
        {
            try
            {
                var backupFolder = await GetBackupFolderAsync();
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"MyGudangDB_Backup_{timestamp}.bak";
                var filePath = Path.Combine(backupFolder, fileName);

                var connString = _config.GetConnectionString("DefaultConnection");
                using var conn = new SqlConnection(connString);
                await conn.OpenAsync();

                var sql = $"BACKUP DATABASE [MyGudangDB] TO DISK = N'{filePath}' WITH FORMAT, INIT, NAME = N'MyGudangDB-Full-{timestamp}'";
                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 120;
                await cmd.ExecuteNonQueryAsync();

                // Update last backup time
                var setting = await _context.BackupSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
                if (setting != null)
                {
                    setting.LastBackupAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"Backup berhasil dibuat: {fileName}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Gagal membuat backup: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                TempData["Error"] = "File backup tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var backupFolder = await GetBackupFolderAsync();
                var filePath = Path.Combine(backupFolder, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = "File backup tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                var connString = _config.GetConnectionString("DefaultConnection");
                using var conn = new SqlConnection(connString);
                await conn.OpenAsync();

                // Set database to single user mode to disconnect all other users
                using (var cmd1 = new SqlCommand("ALTER DATABASE [MyGudangDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", conn))
                {
                    cmd1.CommandTimeout = 30;
                    await cmd1.ExecuteNonQueryAsync();
                }

                // Restore
                var sql = $"RESTORE DATABASE [MyGudangDB] FROM DISK = N'{filePath}' WITH REPLACE";
                using (var cmd2 = new SqlCommand(sql, conn))
                {
                    cmd2.CommandTimeout = 120;
                    await cmd2.ExecuteNonQueryAsync();
                }

                // Set back to multi user
                using (var cmd3 = new SqlCommand("ALTER DATABASE [MyGudangDB] SET MULTI_USER", conn))
                {
                    cmd3.CommandTimeout = 30;
                    await cmd3.ExecuteNonQueryAsync();
                }

                TempData["Success"] = $"Database berhasil di-restore dari: {fileName}";
            }
            catch (Exception ex)
            {
                // Try to set back to multi user in case of error
                try
                {
                    var connString = _config.GetConnectionString("DefaultConnection");
                    using var conn = new SqlConnection(connString);
                    await conn.OpenAsync();
                    using var cmd = new SqlCommand("ALTER DATABASE [MyGudangDB] SET MULTI_USER", conn);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch { }

                TempData["Error"] = $"Gagal restore database: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return NotFound();

            var backupFolder = await GetBackupFolderAsync();
            var filePath = Path.Combine(backupFolder, fileName);

            if (!System.IO.File.Exists(filePath)) return NotFound();

            var bytes = System.IO.File.ReadAllBytes(filePath);
            return File(bytes, "application/octet-stream", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBackup(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                TempData["Error"] = "File tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            var backupFolder = await GetBackupFolderAsync();
            var filePath = Path.Combine(backupFolder, fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                TempData["Success"] = $"File backup {fileName} berhasil dihapus.";
            }
            else
            {
                TempData["Error"] = "File tidak ditemukan.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAutoBackup()
        {
            var setting = await _context.BackupSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (setting != null)
            {
                setting.AutoBackupEnabled = !setting.AutoBackupEnabled;
                await _context.SaveChangesAsync();
                TempData["Success"] = setting.AutoBackupEnabled
                    ? "Auto backup telah diaktifkan."
                    : "Auto backup telah dinonaktifkan.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInterval(int hours)
        {
            if (hours < 1) hours = 1;
            if (hours > 168) hours = 168; // max 1 week

            var setting = await _context.BackupSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (setting != null)
            {
                setting.IntervalHours = hours;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Interval auto backup diperbarui menjadi {hours} jam.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePath(string backupPath)
        {
            var setting = await _context.BackupSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (setting != null)
            {
                setting.BackupPath = string.IsNullOrWhiteSpace(backupPath) ? "wwwroot/backups" : backupPath;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Lokasi backup berhasil diperbarui.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
