using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using itam.Data;

namespace itam.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class ActivityLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? module, DateTime? from, DateTime? to)
        {
            var query = _context.ActivityLogs.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(a => a.Detail!.Contains(search) || a.UserName.Contains(search));
            if (!string.IsNullOrEmpty(module))
                query = query.Where(a => a.Module == module);
            if (from.HasValue)
                query = query.Where(a => a.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(a => a.CreatedAt <= to.Value.AddDays(1));

            ViewBag.Modules = await _context.ActivityLogs.Select(a => a.Module).Distinct().OrderBy(m => m).ToListAsync();
            ViewBag.Search = search;
            ViewBag.Module = module;
            ViewBag.From = from;
            ViewBag.To = to;

            var logs = await query.OrderByDescending(a => a.CreatedAt).Take(500).ToListAsync();
            return View(logs);
        }
    }
}
