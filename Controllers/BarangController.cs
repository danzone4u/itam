using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;
using ClosedXML.Excel;

namespace MyGudang.Controllers
{
    [Authorize]
    public class BarangController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BarangController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(string? search, int? kategoriId)
        {
            var query = _context.Barangs.Include(b => b.Kategori).Include(b => b.Supplier).AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.NamaBarang.Contains(search) || b.KodeBarang.Contains(search));
                ViewBag.Search = search;
            }
            if (kategoriId.HasValue)
            {
                query = query.Where(b => b.KategoriId == kategoriId);
                ViewBag.KategoriId = kategoriId;
            }
            ViewBag.Kategoris = new SelectList(await _context.Kategoris.ToListAsync(), "Id", "NamaKategori", kategoriId);
            var barangList = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

            // Load ruangan per barang
            var lokasiData = await _context.BarangLokasis
                .Include(bl => bl.Lokasi)
                .Where(bl => bl.Stok > 0)
                .ToListAsync();
            ViewBag.LokasiPerBarang = lokasiData
                .GroupBy(bl => bl.BarangId)
                .ToDictionary(g => g.Key, g => g.Select(bl => bl.Lokasi!.NamaLokasi).ToList());

            return View(barangList);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Kategoris = new SelectList(await _context.Kategoris.ToListAsync(), "Id", "NamaKategori");
            ViewBag.Suppliers = new SelectList(await _context.Suppliers.ToListAsync(), "Id", "NamaSupplier");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Barang barang, IFormFile? gambarFile)
        {
            if (ModelState.IsValid)
            {
                if (gambarFile != null && gambarFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(gambarFile.FileName);
                    var path = Path.Combine(_env.WebRootPath, "uploads", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using var stream = new FileStream(path, FileMode.Create);
                    await gambarFile.CopyToAsync(stream);
                    barang.Gambar = "/uploads/" + fileName;
                }
                barang.CreatedAt = DateTime.Now;
                _context.Add(barang);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Barang berhasil ditambahkan!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Kategoris = new SelectList(await _context.Kategoris.ToListAsync(), "Id", "NamaKategori", barang.KategoriId);
            ViewBag.Suppliers = new SelectList(await _context.Suppliers.ToListAsync(), "Id", "NamaSupplier", barang.SupplierId);
            return View(barang);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var barang = await _context.Barangs.FindAsync(id);
            if (barang == null) return NotFound();
            ViewBag.Kategoris = new SelectList(await _context.Kategoris.ToListAsync(), "Id", "NamaKategori", barang.KategoriId);
            ViewBag.Suppliers = new SelectList(await _context.Suppliers.ToListAsync(), "Id", "NamaSupplier", barang.SupplierId);
            return View(barang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Barang barang, IFormFile? gambarFile)
        {
            if (id != barang.Id) return NotFound();
            if (ModelState.IsValid)
            {
                if (gambarFile != null && gambarFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(gambarFile.FileName);
                    var path = Path.Combine(_env.WebRootPath, "uploads", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using var stream = new FileStream(path, FileMode.Create);
                    await gambarFile.CopyToAsync(stream);
                    barang.Gambar = "/uploads/" + fileName;
                }
                _context.Update(barang);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Barang berhasil diperbarui!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Kategoris = new SelectList(await _context.Kategoris.ToListAsync(), "Id", "NamaKategori", barang.KategoriId);
            ViewBag.Suppliers = new SelectList(await _context.Suppliers.ToListAsync(), "Id", "NamaSupplier", barang.SupplierId);
            return View(barang);
        }

        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) return NotFound();

            var barang = await _context.Barangs
                .Include(b => b.Kategori)
                .Include(b => b.Supplier)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (barang == null) return NotFound();

            // Ambil histori masuk
            var historiMasuk = await _context.BarangMasuks
                .Include(bm => bm.Lokasi)
                .Where(bm => bm.BarangId == id)
                .OrderByDescending(bm => bm.TanggalMasuk)
                .ToListAsync();

            // Ambil histori keluar
            var historiKeluar = await _context.BarangKeluars
                .Include(bk => bk.Lokasi)
                .Where(bk => bk.BarangId == id)
                .OrderByDescending(bk => bk.TanggalKeluar)
                .ToListAsync();

            // Ambil histori peminjaman
            var historiPinjam = await _context.Peminjamans
                .Where(p => p.BarangId == id)
                .OrderByDescending(p => p.TanggalPinjam)
                .ToListAsync();

            // Ambil histori kembali
            var historiKembali = await _context.BarangKembalis
                .Where(bk => bk.BarangId == id)
                .OrderByDescending(bk => bk.TanggalKembali)
                .ToListAsync();

            ViewBag.HistoriMasuk = historiMasuk;
            ViewBag.HistoriKeluar = historiKeluar;
            ViewBag.HistoriPinjam = historiPinjam;
            ViewBag.HistoriKembali = historiKembali;

            return View(barang);
        }

        public async Task<IActionResult> ExportExcel()
        {
            var data = await _context.Barangs.Include(b => b.Kategori).Include(b => b.Supplier).OrderBy(b => b.NamaBarang).ToListAsync();
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Barang");
            ws.Cell(1, 1).Value = "No";
            ws.Cell(1, 2).Value = "Kode Barang";
            ws.Cell(1, 3).Value = "Nama Barang";
            ws.Cell(1, 4).Value = "Kategori";
            ws.Cell(1, 5).Value = "Supplier";
            ws.Cell(1, 6).Value = "Satuan";
            ws.Cell(1, 7).Value = "Stok";
            ws.Range("A1:G1").Style.Font.Bold = true;
            ws.Range("A1:G1").Style.Fill.BackgroundColor = XLColor.LightBlue;

            for (int i = 0; i < data.Count; i++)
            {
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = data[i].KodeBarang;
                ws.Cell(i + 2, 3).Value = data[i].NamaBarang;
                ws.Cell(i + 2, 4).Value = data[i].Kategori?.NamaKategori;
                ws.Cell(i + 2, 5).Value = data[i].Supplier?.NamaSupplier;
                ws.Cell(i + 2, 6).Value = data[i].Satuan;
                ws.Cell(i + 2, 7).Value = data[i].Stok;
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DataBarang.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "File tidak boleh kosong!";
                return RedirectToAction(nameof(Index));
            }

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheet(1);
            var rows = ws.RowsUsed().Skip(1);
            int count = 0;

            foreach (var row in rows)
            {
                var kode = row.Cell(2).GetString();
                var nama = row.Cell(3).GetString();
                if (string.IsNullOrWhiteSpace(nama)) continue;

                var kategoriName = row.Cell(4).GetString();
                var supplierName = row.Cell(5).GetString();

                var kategori = await _context.Kategoris.FirstOrDefaultAsync(k => k.NamaKategori == kategoriName);
                var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.NamaSupplier == supplierName);

                if (kategori == null || supplier == null) continue;

                _context.Barangs.Add(new Barang
                {
                    KodeBarang = kode,
                    NamaBarang = nama,
                    KategoriId = kategori.Id,
                    SupplierId = supplier.Id,
                    Satuan = row.Cell(6).GetString(),
                    Stok = (int)row.Cell(7).GetDouble(),
                    CreatedAt = DateTime.Now
                });
                count++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{count} data barang berhasil diimport!";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax(string? kodeBarang, string namaBarang, int? kategoriId, int? supplierId, string satuan)
        {
            if (string.IsNullOrWhiteSpace(namaBarang))
                return Json(new { success = false, message = "Nama barang wajib diisi!" });

            // Auto-generate kode if not provided
            if (string.IsNullOrWhiteSpace(kodeBarang) && kategoriId.HasValue && kategoriId.Value > 0)
            {
                kodeBarang = await GenerateKodeBarang(kategoriId.Value);
            }

            var barang = new Barang
            {
                KodeBarang = kodeBarang ?? "",
                NamaBarang = namaBarang,
                KategoriId = kategoriId ?? 0,
                SupplierId = supplierId ?? 0,
                Satuan = satuan ?? "Unit",
                Stok = 0,
                CreatedAt = DateTime.Now
            };
            _context.Barangs.Add(barang);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = barang.Id, nama = barang.NamaBarang, kode = barang.KodeBarang });
        }

        [HttpGet]
        public async Task<IActionResult> GetNextKode(int kategoriId)
        {
            var kode = await GenerateKodeBarang(kategoriId);
            return Json(new { kode });
        }

        private async Task<string> GenerateKodeBarang(int kategoriId)
        {
            var kategori = await _context.Kategoris.FindAsync(kategoriId);
            if (kategori == null) return "";

            var prefix = !string.IsNullOrWhiteSpace(kategori.KodePrefix)
                ? kategori.KodePrefix.ToUpper().Trim()
                : kategori.NamaKategori.Length >= 3
                    ? kategori.NamaKategori.Substring(0, 3).ToUpper()
                    : kategori.NamaKategori.ToUpper();

            var lastBarang = await _context.Barangs
                .Where(b => b.KategoriId == kategoriId && b.KodeBarang.StartsWith(prefix + "-"))
                .OrderByDescending(b => b.KodeBarang)
                .FirstOrDefaultAsync();

            int nextNum = 1;
            if (lastBarang != null)
            {
                var parts = lastBarang.KodeBarang.Split('-');
                if (parts.Length > 1 && int.TryParse(parts.Last(), out int num))
                    nextNum = num + 1;
            }

            return $"{prefix}-{nextNum:D3}";
        }
    }
}
