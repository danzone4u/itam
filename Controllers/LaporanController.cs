using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using ClosedXML.Excel;

namespace MyGudang.Controllers
{
    [Authorize]
    public class LaporanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LaporanController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Kategoris = await _context.Kategoris.OrderBy(k => k.NamaKategori).ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportBarangMasuk(DateTime? from, DateTime? to)
        {
            from ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            to ??= DateTime.Now;

            var data = await _context.BarangMasuks
                .Include(bm => bm.Barang).ThenInclude(b => b!.Kategori)
                .Include(bm => bm.Barang).ThenInclude(b => b!.Supplier)
                .Include(bm => bm.Lokasi)
                .Where(bm => bm.TanggalMasuk >= from && bm.TanggalMasuk <= to.Value.AddDays(1))
                .OrderBy(bm => bm.TanggalMasuk)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Barang Masuk");
            ws.Cell(1, 1).Value = $"Laporan Barang Masuk: {from.Value:dd/MM/yyyy} - {to.Value:dd/MM/yyyy}";
            ws.Range("A1:G1").Merge().Style.Font.Bold = true;

            var row = 3;
            ws.Cell(row, 1).Value = "No"; ws.Cell(row, 2).Value = "Tanggal"; ws.Cell(row, 3).Value = "Kode Barang";
            ws.Cell(row, 4).Value = "Nama Barang"; ws.Cell(row, 5).Value = "Jumlah"; ws.Cell(row, 6).Value = "Harga Satuan";
            ws.Cell(row, 7).Value = "Lokasi"; ws.Cell(row, 8).Value = "Keterangan";
            ws.Range(row, 1, row, 8).Style.Font.Bold = true;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.TanggalMasuk.ToString("dd/MM/yyyy");
                ws.Cell(row, 3).Value = item.Barang?.KodeBarang;
                ws.Cell(row, 4).Value = item.Barang?.NamaBarang;
                ws.Cell(row, 5).Value = item.Jumlah;
                ws.Cell(row, 6).Value = item.HargaSatuan ?? 0;
                ws.Cell(row, 7).Value = item.Lokasi?.NamaLokasi ?? "-";
                ws.Cell(row, 8).Value = item.Keterangan;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Laporan_BarangMasuk_{from.Value:yyyyMMdd}_{to.Value:yyyyMMdd}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportBarangKeluar(DateTime? from, DateTime? to)
        {
            from ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            to ??= DateTime.Now;

            var data = await _context.BarangKeluars
                .Include(bk => bk.Barang).ThenInclude(b => b!.Kategori)
                .Where(bk => bk.TanggalKeluar >= from && bk.TanggalKeluar <= to.Value.AddDays(1))
                .OrderBy(bk => bk.TanggalKeluar)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Barang Keluar");
            ws.Cell(1, 1).Value = $"Laporan Barang Keluar: {from.Value:dd/MM/yyyy} - {to.Value:dd/MM/yyyy}";
            ws.Range("A1:G1").Merge().Style.Font.Bold = true;

            var row = 3;
            ws.Cell(row, 1).Value = "No"; ws.Cell(row, 2).Value = "Tanggal"; ws.Cell(row, 3).Value = "Kode Barang";
            ws.Cell(row, 4).Value = "Nama Barang"; ws.Cell(row, 5).Value = "Jumlah"; ws.Cell(row, 6).Value = "Penerima";
            ws.Cell(row, 7).Value = "No Surat Jalan"; ws.Cell(row, 8).Value = "Keterangan";
            ws.Range(row, 1, row, 8).Style.Font.Bold = true;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.TanggalKeluar.ToString("dd/MM/yyyy");
                ws.Cell(row, 3).Value = item.Barang?.KodeBarang;
                ws.Cell(row, 4).Value = item.Barang?.NamaBarang;
                ws.Cell(row, 5).Value = item.Jumlah;
                ws.Cell(row, 6).Value = item.Penerima;
                ws.Cell(row, 7).Value = item.NoSuratJalan;
                ws.Cell(row, 8).Value = item.Keterangan;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Laporan_BarangKeluar_{from.Value:yyyyMMdd}_{to.Value:yyyyMMdd}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportStokBarang()
        {
            var data = await _context.Barangs
                .Include(b => b.Kategori)
                .Include(b => b.Supplier)
                .OrderBy(b => b.NamaBarang)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Stok Barang");
            ws.Cell(1, 1).Value = $"Laporan Stok Barang - {DateTime.Now:dd/MM/yyyy}";
            ws.Range("A1:G1").Merge().Style.Font.Bold = true;

            var row = 3;
            ws.Cell(row, 1).Value = "No"; ws.Cell(row, 2).Value = "Kode"; ws.Cell(row, 3).Value = "Nama Barang";
            ws.Cell(row, 4).Value = "Kategori"; ws.Cell(row, 5).Value = "Supplier"; ws.Cell(row, 6).Value = "Satuan";
            ws.Cell(row, 7).Value = "Stok"; ws.Cell(row, 8).Value = "Stok Min"; ws.Cell(row, 9).Value = "Status";
            ws.Range(row, 1, row, 9).Style.Font.Bold = true;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.KodeBarang;
                ws.Cell(row, 3).Value = item.NamaBarang;
                ws.Cell(row, 4).Value = item.Kategori?.NamaKategori;
                ws.Cell(row, 5).Value = item.Supplier?.NamaSupplier;
                ws.Cell(row, 6).Value = item.Satuan;
                ws.Cell(row, 7).Value = item.Stok;
                ws.Cell(row, 8).Value = item.StokMinimum;
                ws.Cell(row, 9).Value = item.Stok <= item.StokMinimum ? "PERLU RESTOCK" : "Aman";

                if (item.Stok <= item.StokMinimum)
                    ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.LightSalmon;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Laporan_Stok_{DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}
