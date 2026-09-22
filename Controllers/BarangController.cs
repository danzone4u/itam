using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;
using ClosedXML.Excel;

namespace itam.Controllers
{
    [Authorize(Roles = "SuperAdmin,AdminGudang")]
    public class BarangController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly Services.IStokSyncService _stokSync;

        public BarangController(ApplicationDbContext context, IWebHostEnvironment env, Services.IStokSyncService stokSync)
        {
            _context = context;
            _env = env;
            _stokSync = stokSync;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetData()
        {
            try 
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumnIdx = Request.Form["order[0][column]"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + sortColumnIdx + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                int pageSize = length != null ? Convert.ToInt32(length) : 10;
                int skip = start != null ? Convert.ToInt32(start) : 0;

                var baseQuery = _context.Barangs.AsNoTracking();
                int totalRecords = await baseQuery.CountAsync();

                var query = baseQuery.Include(b => b.Kategori).AsQueryable();

                // Search
                if (!string.IsNullOrEmpty(searchValue))
                { 
                    query = query.Where(m => m.NamaBarang.Contains(searchValue) 
                                          || m.KodeBarang.Contains(searchValue)
                                          || (m.Kategori != null && m.Kategori.NamaKategori.Contains(searchValue)));
                }

                // Total records after filter
                int filteredRecords = await query.CountAsync();

                // Sorting
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    // Simple manual sorting to avoid complex expression building for now
                    switch (sortColumn)
                    {
                        case "NamaBarang":
                            query = sortColumnDirection == "asc" ? query.OrderBy(b => b.NamaBarang) : query.OrderByDescending(b => b.NamaBarang);
                            break;
                        case "KodeBarang":
                            query = sortColumnDirection == "asc" ? query.OrderBy(b => b.KodeBarang) : query.OrderByDescending(b => b.KodeBarang);
                            break;
                        case "Kategori":
                            query = sortColumnDirection == "asc" ? query.OrderBy(b => b.Kategori != null ? b.Kategori.NamaKategori : "") : query.OrderByDescending(b => b.Kategori != null ? b.Kategori.NamaKategori : "");
                            break;
                        case "Stok":
                            query = sortColumnDirection == "asc" ? query.OrderBy(b => b.Stok) : query.OrderByDescending(b => b.Stok);
                            break;
                        case "UpdatedAt":
                            query = sortColumnDirection == "asc" ? query.OrderBy(b => b.UpdatedAt) : query.OrderByDescending(b => b.UpdatedAt);
                            break;
                        default:
                            query = query.OrderByDescending(b => b.CreatedAt);
                            break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(b => b.CreatedAt);
                }

                // Pagination
                var data = await query.Skip(skip).Take(pageSize).ToListAsync();

                // Load locations only for the current page items
                var itemIds = data.Select(d => d.Id).ToList();
                var lokasiData = await _context.BarangLokasis
                    .Include(bl => bl.Lokasi)
                    .Where(bl => itemIds.Contains(bl.BarangId) && bl.Stok > 0)
                    .ToListAsync();

                var lokasiPerBarang = lokasiData
                    .GroupBy(bl => bl.BarangId)
                    .ToDictionary(g => g.Key, g => g.Select(bl => bl.Lokasi!.NamaLokasi).ToList());

                int drawCount = int.TryParse(draw, out int d) ? d : 0;

                var jsonData = new
                {
                    draw = drawCount,
                    recordsFiltered = filteredRecords,
                    recordsTotal = totalRecords,
                    data = data.Select((item, index) => new
                    {
                        id = item.Id,
                        kodeBarang = item.KodeBarang,
                        namaBarang = item.NamaBarang,
                        kategori = item.Kategori?.NamaKategori ?? "-",
                        satuan = item.Satuan,
                        stok = item.Stok,
                        stokMinimum = item.StokMinimum,
                        lokasi = lokasiPerBarang.ContainsKey(item.Id) ? lokasiPerBarang[item.Id] : new List<string>(),
                        updatedAt = item.UpdatedAt.ToString("dd/MM/yyyy HH:mm"),
                        no = skip + index + 1
                    })
                };

                return Json(jsonData);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Server error: " + ex.Message });
            }
        }

        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Kategoris = new SelectList(await _context.Kategoris.ToListAsync(), "Id", "NamaKategori");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
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
            return View(barang);
        }

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var barang = await _context.Barangs.FindAsync(id);
            if (barang == null) return NotFound();
            ViewBag.Kategoris = new SelectList(await _context.Kategoris.ToListAsync(), "Id", "NamaKategori", barang.KategoriId);
            return View(barang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
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
            return View(barang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var barang = await _context.Barangs.FindAsync(id);
            if (barang == null) return NotFound();

            await DeleteBarangAndHistoryAsync(id);
            TempData["Success"] = "Data barang dan historinya berhasil dihapus.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> BulkDelete(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "Pilih minimal satu data untuk dihapus.";
                return RedirectToAction(nameof(Index));
            }

            int count = 0;
            foreach (var id in ids)
            {
                var barang = await _context.Barangs.FindAsync(id);
                if (barang != null)
                {
                    await DeleteBarangAndHistoryAsync(id);
                    count++;
                }
            }
            
            TempData["Success"] = $"{count} data barang dan historinya berhasil dihapus.";
            return RedirectToAction(nameof(Index));
        }

        private async Task DeleteBarangAndHistoryAsync(int barangId)
        {
            // 1. Peminjamans
            var peminjamans = await _context.Peminjamans.Where(p => p.BarangId == barangId).ToListAsync();
            _context.Peminjamans.RemoveRange(peminjamans);

            // 2. TranserBarangSerials and TransferBarangs
            var transferBarangs = await _context.TransferBarangs.Where(t => t.BarangId == barangId).ToListAsync();
            var tbIds = transferBarangs.Select(t => t.Id).ToList();
            var tbSerials = await _context.TransferBarangSerials.Where(ts => tbIds.Contains(ts.TransferBarangId)).ToListAsync();
            _context.TransferBarangSerials.RemoveRange(tbSerials);
            _context.TransferBarangs.RemoveRange(transferBarangs);

            // 3. StokOpnameDetails
            var soDetails = await _context.StokOpnameDetails.Where(s => s.BarangId == barangId).ToListAsync();
            _context.StokOpnameDetails.RemoveRange(soDetails);

            // 4. BarangSerials (Must be deleted before Masuk, Keluar, Kembali due to Restrict FKs)
            var serials = await _context.BarangSerials.Where(s => s.BarangId == barangId).ToListAsync();
            _context.BarangSerials.RemoveRange(serials);

            // 5. BarangKembalis
            var kembalis = await _context.BarangKembalis.Where(k => k.BarangId == barangId).ToListAsync();
            _context.BarangKembalis.RemoveRange(kembalis);

            // 6. BarangKeluars
            var keluars = await _context.BarangKeluars.Where(k => k.BarangId == barangId).ToListAsync();
            _context.BarangKeluars.RemoveRange(keluars);

            // 7. BarangMasuks
            var masuks = await _context.BarangMasuks.Where(m => m.BarangId == barangId).ToListAsync();
            _context.BarangMasuks.RemoveRange(masuks);

            // 8. BarangLokasis
            var lokasis = await _context.BarangLokasis.Where(l => l.BarangId == barangId).ToListAsync();
            _context.BarangLokasis.RemoveRange(lokasis);

            // 9. Barang
            var barang = await _context.Barangs.FindAsync(barangId);
            if (barang != null) _context.Barangs.Remove(barang);

            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) return NotFound();

            var barang = await _context.Barangs
                .Include(b => b.Kategori)
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

            // Ambil daftar S/N yang tersedia saat ini
            var snTersedia = await _context.BarangSerials
                .Where(s => s.BarangId == id && s.Status == "Tersedia")
                .OrderBy(s => s.SerialNumber)
                .ToListAsync();

            ViewBag.HistoriMasuk = historiMasuk;
            ViewBag.HistoriKeluar = historiKeluar;
            ViewBag.HistoriPinjam = historiPinjam;
            ViewBag.HistoriKembali = historiKembali;
            ViewBag.SnTersedia = snTersedia;

            return View(barang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> SyncStok(int id)
        {
            var newStok = await _stokSync.SyncBarangAsync(id);
            TempData["Success"] = $"Stok & Serial Number barang berhasil disinkronkan sesuai riwayat transaksi! (Stok saat ini: {newStok})";
            return RedirectToAction(nameof(Detail), new { id });
        }

        public async Task<IActionResult> ExportExcel()
        {
            var data = await _context.Barangs.Include(b => b.Kategori).OrderBy(b => b.NamaBarang).ToListAsync();
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Barang");
            ws.Cell(1, 1).Value = "No";
            ws.Cell(1, 2).Value = "Kode Barang";
            ws.Cell(1, 3).Value = "Nama Barang";
            ws.Cell(1, 4).Value = "Kategori";
            ws.Cell(1, 5).Value = "Satuan";
            ws.Cell(1, 6).Value = "Stok";
            ws.Range("A1:F1").Style.Font.Bold = true;
            ws.Range("A1:F1").Style.Fill.BackgroundColor = XLColor.LightBlue;

            for (int i = 0; i < data.Count; i++)
            {
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = data[i].KodeBarang;
                ws.Cell(i + 2, 3).Value = data[i].NamaBarang;
                ws.Cell(i + 2, 4).Value = data[i].Kategori?.NamaKategori;
                ws.Cell(i + 2, 5).Value = data[i].Satuan;
                ws.Cell(i + 2, 6).Value = data[i].Stok;
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DataBarang.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
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

            var nextNumbers = new Dictionary<string, int>();

            foreach (var row in rows)
            {
                var kode = row.Cell(2).GetString();
                var nama = row.Cell(3).GetString();
                if (string.IsNullOrWhiteSpace(nama)) continue;

                var kategoriName = row.Cell(4).GetString();

                var kategori = await _context.Kategoris.FirstOrDefaultAsync(k => k.NamaKategori == kategoriName);

                if (kategori == null) continue;

                if (string.IsNullOrWhiteSpace(kode))
                {
                    var prefix = GetPrefixFromKategori(kategori);

                    if (!nextNumbers.ContainsKey(prefix))
                    {
                        var lastBarang = await _context.Barangs
                            .Where(b => b.KategoriId == kategori.Id && b.KodeBarang.StartsWith(prefix + "-"))
                            .OrderByDescending(b => b.KodeBarang)
                            .FirstOrDefaultAsync();

                        int nextNum = 1;
                        if (lastBarang != null)
                        {
                            var parts = lastBarang.KodeBarang.Split('-');
                            if (parts.Length > 1 && int.TryParse(parts.Last(), out int num))
                                nextNum = num + 1;
                        }
                        nextNumbers[prefix] = nextNum;
                    }

                    kode = $"{prefix}-{nextNumbers[prefix]:D3}";
                    nextNumbers[prefix]++;
                }

                _context.Barangs.Add(new Barang
                {
                    KodeBarang = kode,
                    NamaBarang = nama,
                    KategoriId = kategori.Id,
                    Satuan = row.Cell(5).GetString(),
                    Stok = (int)row.Cell(6).GetDouble(),
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
        [Authorize(Roles = "SuperAdmin,AdminGudang")]
        public async Task<IActionResult> CreateAjax(string? kodeBarang, string namaBarang, int? kategoriId, string satuan)
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

        private static string GetPrefixFromKategori(Kategori kategori)
        {
            if (!string.IsNullOrWhiteSpace(kategori.KodePrefix))
                return kategori.KodePrefix.ToUpper().Trim();

            var words = kategori.NamaKategori.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
                return string.Concat(words.Select(w => w[0])).ToUpper();

            return kategori.NamaKategori.Length >= 3
                ? kategori.NamaKategori.Substring(0, 3).ToUpper()
                : kategori.NamaKategori.ToUpper();
        }

        private async Task<string> GenerateKodeBarang(int kategoriId)
        {
            var kategori = await _context.Kategoris.FindAsync(kategoriId);
            if (kategori == null) return "";

            var prefix = GetPrefixFromKategori(kategori);

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
