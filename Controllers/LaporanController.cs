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
            ws.Cell(row, 7).Value = "PIC"; ws.Cell(row, 8).Value = "No Surat Jalan"; ws.Cell(row, 9).Value = "Keterangan";
            ws.Range(row, 1, row, 9).Style.Font.Bold = true;

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
                ws.Cell(row, 7).Value = item.Pic;
                ws.Cell(row, 8).Value = item.NoSuratJalan;
                ws.Cell(row, 9).Value = item.Keterangan;
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

        [HttpGet]
        public async Task<IActionResult> ExportPeminjaman(DateTime? from, DateTime? to)
        {
            from ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            to ??= DateTime.Now;

            var data = await _context.Peminjamans
                .Include(p => p.Barang).ThenInclude(b => b!.Kategori)
                .Where(p => p.TanggalPinjam >= from && p.TanggalPinjam <= to.Value.AddDays(1))
                .OrderBy(p => p.TanggalPinjam)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Peminjaman");
            ws.Cell(1, 1).Value = $"Laporan Peminjaman Barang: {from.Value:dd/MM/yyyy} - {to.Value:dd/MM/yyyy}";
            ws.Range("A1:J1").Merge().Style.Font.Bold = true;

            var row = 3;
            ws.Cell(row, 1).Value = "No"; ws.Cell(row, 2).Value = "No. Peminjaman"; ws.Cell(row, 3).Value = "Tanggal Pinjam";
            ws.Cell(row, 4).Value = "Kode Barang"; ws.Cell(row, 5).Value = "Nama Barang"; ws.Cell(row, 6).Value = "Peminjam";
            ws.Cell(row, 7).Value = "Departemen"; ws.Cell(row, 8).Value = "Jatuh Tempo"; ws.Cell(row, 9).Value = "Status";
            ws.Cell(row, 10).Value = "Keterangan";
            ws.Range(row, 1, row, 10).Style.Font.Bold = true;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.NoPeminjaman;
                ws.Cell(row, 3).Value = item.TanggalPinjam.ToString("dd/MM/yyyy");
                ws.Cell(row, 4).Value = item.Barang?.KodeBarang;
                ws.Cell(row, 5).Value = item.Barang?.NamaBarang;
                ws.Cell(row, 6).Value = item.Peminjam;
                ws.Cell(row, 7).Value = item.Departemen;
                ws.Cell(row, 8).Value = item.TanggalJatuhTempo.ToString("dd/MM/yyyy");
                ws.Cell(row, 9).Value = item.Status;
                ws.Cell(row, 10).Value = item.Keterangan;

                if (item.Status == "Dipinjam" && item.TanggalJatuhTempo < DateTime.Now)
                    ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.LightSalmon;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Laporan_Peminjaman_{from.Value:yyyyMMdd}_{to.Value:yyyyMMdd}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportBarangKembali(DateTime? from, DateTime? to)
        {
            from ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            to ??= DateTime.Now;

            var data = await _context.BarangKembalis
                .Include(bk => bk.Barang).ThenInclude(b => b!.Kategori)
                .Include(bk => bk.BarangKeluar)
                .Where(bk => bk.TanggalKembali >= from && bk.TanggalKembali <= to.Value.AddDays(1))
                .OrderBy(bk => bk.TanggalKembali)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Pengembalian Barang");
            ws.Cell(1, 1).Value = $"Laporan Pengembalian Barang: {from.Value:dd/MM/yyyy} - {to.Value:dd/MM/yyyy}";
            ws.Range("A1:I1").Merge().Style.Font.Bold = true;

            var row = 3;
            ws.Cell(row, 1).Value = "No"; ws.Cell(row, 2).Value = "Tanggal Kembali"; ws.Cell(row, 3).Value = "Kode Barang";
            ws.Cell(row, 4).Value = "Nama Barang"; ws.Cell(row, 5).Value = "Jumlah"; ws.Cell(row, 6).Value = "Dikembalikan Oleh";
            ws.Cell(row, 7).Value = "Kondisi"; ws.Cell(row, 8).Value = "Ref. Barang Keluar"; ws.Cell(row, 9).Value = "Keterangan";
            ws.Range(row, 1, row, 9).Style.Font.Bold = true;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.TanggalKembali.ToString("dd/MM/yyyy");
                ws.Cell(row, 3).Value = item.Barang?.KodeBarang;
                ws.Cell(row, 4).Value = item.Barang?.NamaBarang;
                ws.Cell(row, 5).Value = item.Jumlah;
                ws.Cell(row, 6).Value = item.DikembalikanOleh;
                ws.Cell(row, 7).Value = item.Kondisi;
                ws.Cell(row, 8).Value = item.BarangKeluar?.NoSuratJalan ?? "-";
                ws.Cell(row, 9).Value = item.Keterangan;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Laporan_Pengembalian_{from.Value:yyyyMMdd}_{to.Value:yyyyMMdd}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPeremajaan(DateTime? from, DateTime? to)
        {
            from ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            to ??= DateTime.Now;

            var data = await _context.Peremajaans
                .Include(p => p.Barang).ThenInclude(b => b!.Kategori)
                .Include(p => p.BarangKeluar)
                .Where(p => p.TanggalPeremajaan >= from && p.TanggalPeremajaan <= to.Value.AddDays(1))
                .OrderBy(p => p.TanggalPeremajaan)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Peremajaan Barang");
            ws.Cell(1, 1).Value = $"Laporan Peremajaan Barang: {from.Value:dd/MM/yyyy} - {to.Value:dd/MM/yyyy}";
            ws.Range("A1:I1").Merge().Style.Font.Bold = true;

            var row = 3;
            ws.Cell(row, 1).Value = "No"; ws.Cell(row, 2).Value = "Tanggal Peremajaan"; ws.Cell(row, 3).Value = "Kode Barang";
            ws.Cell(row, 4).Value = "Nama Barang"; ws.Cell(row, 5).Value = "Jumlah"; ws.Cell(row, 6).Value = "Dikembalikan Oleh";
            ws.Cell(row, 7).Value = "Kondisi"; ws.Cell(row, 8).Value = "Tindak Lanjut"; ws.Cell(row, 9).Value = "Keterangan";
            ws.Range(row, 1, row, 9).Style.Font.Bold = true;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.TanggalPeremajaan.ToString("dd/MM/yyyy");
                ws.Cell(row, 3).Value = item.Barang?.KodeBarang;
                ws.Cell(row, 4).Value = item.Barang?.NamaBarang;
                ws.Cell(row, 5).Value = item.Jumlah;
                ws.Cell(row, 6).Value = item.DikembalikanOleh;
                ws.Cell(row, 7).Value = item.Kondisi;
                ws.Cell(row, 8).Value = item.TindakLanjut;
                ws.Cell(row, 9).Value = item.Keterangan;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Laporan_Peremajaan_{from.Value:yyyyMMdd}_{to.Value:yyyyMMdd}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportTransferBarang(DateTime? from, DateTime? to)
        {
            from ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            to ??= DateTime.Now;

            var data = await _context.TransferBarangs
                .Include(t => t.Barang).ThenInclude(b => b!.Kategori)
                .Include(t => t.DariLokasi)
                .Include(t => t.KeLokasi)
                .Where(t => t.TanggalTransfer >= from && t.TanggalTransfer <= to.Value.AddDays(1))
                .OrderBy(t => t.TanggalTransfer)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Transfer Barang");
            ws.Cell(1, 1).Value = $"Laporan Transfer Ruangan: {from.Value:dd/MM/yyyy} - {to.Value:dd/MM/yyyy}";
            ws.Range("A1:J1").Merge().Style.Font.Bold = true;

            var row = 3;
            ws.Cell(row, 1).Value = "No"; ws.Cell(row, 2).Value = "No. Transfer"; ws.Cell(row, 3).Value = "Tanggal";
            ws.Cell(row, 4).Value = "Kode Barang"; ws.Cell(row, 5).Value = "Nama Barang"; ws.Cell(row, 6).Value = "Jumlah";
            ws.Cell(row, 7).Value = "Dari Ruangan"; ws.Cell(row, 8).Value = "Ke Ruangan"; ws.Cell(row, 9).Value = "Keterangan";
            ws.Range(row, 1, row, 9).Style.Font.Bold = true;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.NoTransfer;
                ws.Cell(row, 3).Value = item.TanggalTransfer.ToString("dd/MM/yyyy");
                ws.Cell(row, 4).Value = item.Barang?.KodeBarang;
                ws.Cell(row, 5).Value = item.Barang?.NamaBarang;
                ws.Cell(row, 6).Value = item.Jumlah;
                ws.Cell(row, 7).Value = item.DariLokasi?.NamaLokasi;
                ws.Cell(row, 8).Value = item.KeLokasi?.NamaLokasi;
                ws.Cell(row, 9).Value = item.Keterangan;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Laporan_TransferRuangan_{from.Value:yyyyMMdd}_{to.Value:yyyyMMdd}.xlsx");
        }
    }
}
