using MyGudang.Data;
using MyGudang.Models;

namespace MyGudang.Services
{
    public static class ActivityLogger
    {
        public static async Task LogAsync(ApplicationDbContext context, string userName, string action, string module, string? detail, string? ipAddress = null)
        {
            context.ActivityLogs.Add(new ActivityLog
            {
                UserName = userName,
                Action = action,
                Module = module,
                Detail = detail,
                IpAddress = ipAddress,
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();
        }
    }
}
