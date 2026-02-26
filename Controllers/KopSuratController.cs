using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;

namespace MyGudang.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class KopSuratController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KopSuratController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (kop == null)
            {
                kop = new KopSurat();
                _context.KopSurats.Add(kop);
                await _context.SaveChangesAsync();
            }
            return View(kop);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(KopSurat model)
        {
            var kop = await _context.KopSurats.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (kop == null)
            {
                _context.KopSurats.Add(model);
            }
            else
            {
                kop.NamaPerusahaan = model.NamaPerusahaan;
                kop.SubJudul = model.SubJudul;
                kop.Alamat = model.Alamat;
                kop.Telepon = model.Telepon;
                kop.Email = model.Email;
                kop.Website = model.Website;
                kop.NamaPengirim = model.NamaPengirim;
                kop.JabatanPengirim = model.JabatanPengirim;
                kop.TampilkanLogo = model.TampilkanLogo;
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Kop surat berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }
    }
}
