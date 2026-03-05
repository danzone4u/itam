using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using itam.Data;

namespace itam.Services
{
    public class BackupService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<BackupService> _logger;
        private Timer? _timer;

        public BackupService(IServiceProvider serviceProvider, IConfiguration config,
            IWebHostEnvironment env, ILogger<BackupService> logger)
        {
            _serviceProvider = serviceProvider;
            _config = config;
            _env = env;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BackupService started.");
            _timer = new Timer(CheckAndBackup, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(30));
            return Task.CompletedTask;
        }

        private async void CheckAndBackup(object? state)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var setting = await context.BackupSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
                if (setting == null || !setting.AutoBackupEnabled) return;

                var now = DateTime.Now;
                if (setting.LastBackupAt.HasValue)
                {
                    var diff = now - setting.LastBackupAt.Value;
                    if (diff.TotalHours < setting.IntervalHours) return;
                }

                // Do backup
                var backupFolder = setting.BackupPath;
                if (string.IsNullOrWhiteSpace(backupFolder))
                    backupFolder = Path.Combine(_env.WebRootPath, "backups");

                try
                {
                    Directory.CreateDirectory(backupFolder);
                }
                catch
                {
                    // Fallback if the user-provided path is invalid or inaccessible
                    backupFolder = Path.Combine(_env.WebRootPath, "backups");
                    Directory.CreateDirectory(backupFolder);
                }

                var timestamp = now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"itamDB_Auto_{timestamp}.bak";
                var filePath = Path.Combine(backupFolder, fileName);

                var connString = _config.GetConnectionString("DefaultConnection");
                using var conn = new SqlConnection(connString);
                await conn.OpenAsync();

                var sql = $"BACKUP DATABASE [itamDB] TO DISK = N'{filePath}' WITH FORMAT, INIT, NAME = N'itamDB-Auto-{timestamp}'";
                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = 120;
                await cmd.ExecuteNonQueryAsync();

                setting.LastBackupAt = now;
                await context.SaveChangesAsync();

                _logger.LogInformation($"Auto backup created: {fileName}");

                // Cleanup: keep only last 10 auto backups
                var autoFiles = Directory.GetFiles(backupFolder, "itamDB_Auto_*.bak")
                    .OrderByDescending(f => f)
                    .Skip(10)
                    .ToList();

                foreach (var oldFile in autoFiles)
                {
                    File.Delete(oldFile);
                    _logger.LogInformation($"Old auto backup deleted: {Path.GetFileName(oldFile)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto backup failed.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BackupService stopped.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
