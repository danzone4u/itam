using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;

namespace itam.Controllers
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

        // ── Ambil nama database dari connection string ──
        private string GetDatabaseName()
        {
            var connString = _config.GetConnectionString("DefaultConnection") ?? "";
            var builder = new SqlConnectionStringBuilder(connString);
            var dbName = builder.InitialCatalog;
            
            // Validasi nama database agar aman (hanya alfanumerik dan underscore)
            if (!System.Text.RegularExpressions.Regex.IsMatch(dbName, @"^[a-zA-Z0-9_]+$"))
            {
                throw new InvalidOperationException("Nama database tidak valid atau mengandung karakter berbahaya.");
            }
            
            return dbName;
        }

        // ── Ambil default backup directory dari SQL Server ──
        private async Task<string> GetSqlDefaultBackupDirAsync()
        {
            try
            {
                var connString = _config.GetConnectionString("DefaultConnection");
                using var conn = new SqlConnection(connString);
                await conn.OpenAsync();
                const string sql = "SELECT SERVERPROPERTY('InstanceDefaultBackupPath') AS BackupDir";
                using var cmd = new SqlCommand(sql, conn);
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    var dir = result.ToString()!.TrimEnd('\\');
                    if (Directory.Exists(dir)) return dir;
                }
            }
            catch { }
            return Path.Combine(_env.WebRootPath, "backups");
        }

        private async Task<string> GetBackupFolderAsync()
        {
            var setting = await _context.BackupSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            var path = setting?.BackupPath;

            if (string.IsNullOrWhiteSpace(path) || path == "wwwroot/backups")
            {
                path = await GetSqlDefaultBackupDirAsync();
                if (setting != null)
                {
                    setting.BackupPath = path;
                    await _context.SaveChangesAsync();
                }
            }

            try { Directory.CreateDirectory(path); }
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
                setting = new BackupSetting { BackupPath = await GetSqlDefaultBackupDirAsync() };
                _context.BackupSettings.Add(setting);
                await _context.SaveChangesAsync();
            }

            var backupFolder = await GetBackupFolderAsync();
            List<FileInfo> files = new();
            try
            {
                files = Directory.GetFiles(backupFolder, "*.bak")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();
            }
            catch { }

            ViewBag.Setting     = setting;
            ViewBag.BackupFiles = files;
            ViewBag.CurrentPath = backupFolder;
            ViewBag.DbName      = GetDatabaseName();
            return View();
        }

        // ── API: Browse folder struktur server (untuk folder picker di UI) ──
        [HttpGet]
        public IActionResult BrowseFolders(string? path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    var drives = DriveInfo.GetDrives()
                        .Where(d => d.IsReady)
                        .Select(d => new {
                            name = d.Name.TrimEnd('\\'),
                            path = d.Name.TrimEnd('\\'),
                            hasSubs = true
                        }).ToList<object>();
                    return Json(new { ok = true, items = drives, current = "", breadcrumb = new List<object>() });
                }

                if (!Directory.Exists(path))
                    return Json(new { ok = false, message = "Folder tidak ditemukan" });

                var dirs = new List<object>();
                try
                {
                    dirs = Directory.GetDirectories(path)
                        .Select(d => new DirectoryInfo(d))
                        .Where(d => (d.Attributes & FileAttributes.Hidden) == 0 &&
                                    (d.Attributes & FileAttributes.System) == 0)
                        .OrderBy(d => d.Name)
                        .Select(d =>
                        {
                            bool hasSubs = false;
                            try { hasSubs = Directory.GetDirectories(d.FullName).Length > 0; } catch { }
                            return (object)new { name = d.Name, path = d.FullName, hasSubs };
                        })
                        .ToList();
                }
                catch { }

                // Breadcrumb
                var parts = new List<object>();
                var temp = new DirectoryInfo(path);
                var chain = new List<DirectoryInfo>();
                var cur = temp;
                while (cur != null) { chain.Insert(0, cur); cur = cur.Parent; }
                foreach (var p in chain)
                    parts.Add(new { name = p.Name, path = p.FullName });

                return Json(new { ok = true, items = dirs, current = path, breadcrumb = parts });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBackup()
        {
            try
            {
                var dbName       = GetDatabaseName();
                var backupFolder = await GetBackupFolderAsync();
                var timestamp    = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName     = $"{dbName}_Backup_{timestamp}.bak";
                var filePath     = Path.Combine(backupFolder, fileName);

                var connString = _config.GetConnectionString("DefaultConnection");
                using var conn = new SqlConnection(connString);
                await conn.OpenAsync();

                var sql = $"BACKUP DATABASE [{dbName}] TO DISK = N'{filePath.Replace("'", "''")}' WITH FORMAT, INIT, NAME = N'{dbName}-Full-{timestamp}', STATS = 10";
                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 300;
                await cmd.ExecuteNonQueryAsync();

                var setting = await _context.BackupSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
                if (setting != null) { setting.LastBackupAt = DateTime.Now; await _context.SaveChangesAsync(); }

                TempData["Success"] = $"✅ Backup berhasil disimpan: {fileName}";
            }
            catch (SqlException ex) when (ex.Number == 3201 || ex.Number == 3202 || ex.Number == 4064 || ex.Number == 15)
            {
                TempData["Error"] = $"❌ SQL Server tidak dapat mengakses folder backup. " +
                    $"Coba gunakan folder default SQL Server, atau pastikan akun SQL Server Service memiliki izin Write. Detail: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Backup gagal: {ex.Message}";
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

            var dbName = GetDatabaseName();
            try
            {
                var backupFolder = await GetBackupFolderAsync();
                var filePath     = Path.Combine(backupFolder, Path.GetFileName(fileName));

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = "File backup tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                // Restore harus connect ke master, bukan ke database yang akan di-restore
                var cb = new SqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection")!)
                    { InitialCatalog = "master" };

                using var conn = new SqlConnection(cb.ToString());
                await conn.OpenAsync();

                using (var cmd1 = new SqlCommand($"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", conn))
                { cmd1.CommandTimeout = 30; await cmd1.ExecuteNonQueryAsync(); }

                var sqlR = $"RESTORE DATABASE [{dbName}] FROM DISK = N'{filePath.Replace("'", "''")}' WITH REPLACE, STATS = 10";
                using (var cmd2 = new SqlCommand(sqlR, conn))
                { cmd2.CommandTimeout = 300; await cmd2.ExecuteNonQueryAsync(); }

                using (var cmd3 = new SqlCommand($"ALTER DATABASE [{dbName}] SET MULTI_USER", conn))
                { cmd3.CommandTimeout = 30; await cmd3.ExecuteNonQueryAsync(); }

                TempData["Success"] = $"✅ Database berhasil di-restore dari: {fileName}";
            }
            catch (Exception ex)
            {
                try
                {
                    var cb = new SqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection")!)
                        { InitialCatalog = "master" };
                    using var conn = new SqlConnection(cb.ToString());
                    await conn.OpenAsync();
                    using var cmd = new SqlCommand($"ALTER DATABASE [{dbName}] SET MULTI_USER", conn);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch { }

                TempData["Error"] = $"❌ Gagal restore: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return NotFound();
            var backupFolder = await GetBackupFolderAsync();
            var filePath     = Path.Combine(backupFolder, Path.GetFileName(fileName));
            if (!System.IO.File.Exists(filePath)) return NotFound();
            var bytes = System.IO.File.ReadAllBytes(filePath);
            return File(bytes, "application/octet-stream", Path.GetFileName(filePath));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBackup(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) { TempData["Error"] = "File tidak valid."; return RedirectToAction(nameof(Index)); }
            var backupFolder = await GetBackupFolderAsync();
            var filePath     = Path.Combine(backupFolder, Path.GetFileName(fileName));
            if (System.IO.File.Exists(filePath)) { System.IO.File.Delete(filePath); TempData["Success"] = $"File {fileName} berhasil dihapus."; }
            else TempData["Error"] = "File tidak ditemukan.";
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
                TempData["Success"] = setting.AutoBackupEnabled ? "Auto backup diaktifkan." : "Auto backup dinonaktifkan.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInterval(int hours)
        {
            if (hours < 1) hours = 1;
            if (hours > 168) hours = 168;
            var setting = await _context.BackupSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (setting != null) { setting.IntervalHours = hours; await _context.SaveChangesAsync(); TempData["Success"] = $"Interval diperbarui: {hours} jam."; }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePath(string backupPath)
        {
            if (string.IsNullOrWhiteSpace(backupPath)) { TempData["Error"] = "Path tidak boleh kosong."; return RedirectToAction(nameof(Index)); }
            var setting = await _context.BackupSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (setting != null) { setting.BackupPath = backupPath.Trim(); await _context.SaveChangesAsync(); TempData["Success"] = $"Lokasi backup diperbarui: {backupPath}"; }
            return RedirectToAction(nameof(Index));
        }

        // ── Test apakah SQL Server bisa akses path ──
        [HttpPost]
        public async Task<IActionResult> TestPath([FromBody] TestPathRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Path))
                return Json(new { ok = false, message = "Path kosong" });
            try
            {
                var dbName   = GetDatabaseName();
                var testFile = Path.Combine(req.Path.Trim(), $"_test_{Guid.NewGuid():N}.bak");
                var connString = _config.GetConnectionString("DefaultConnection");
                using var conn = new SqlConnection(connString);
                await conn.OpenAsync();
                var sql = $"BACKUP DATABASE [{dbName}] TO DISK = N'{testFile.Replace("'", "''")}' WITH FORMAT, INIT, NAME = N'test'";
                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 60;
                await cmd.ExecuteNonQueryAsync();
                try { if (System.IO.File.Exists(testFile)) System.IO.File.Delete(testFile); } catch { }
                return Json(new { ok = true, message = "✅ SQL Server dapat mengakses folder ini!" });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = $"❌ Tidak bisa diakses oleh SQL Server: {ex.Message}" });
            }
        }
    }

    public class TestPathRequest { public string? Path { get; set; } }
}
