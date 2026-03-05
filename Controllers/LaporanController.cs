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
                .Include(bm => bm.Supplier)
                .Include(bm => bm.Lokasi)
                .Include(bm => bm.BarangSerials)
                .Where(bm => bm.TanggalMasuk >= from && bm.TanggalMasuk <= to.Value.AddDays(1))
                .OrderBy(bm => bm.TanggalMasuk)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Barang Masuk");
            ws.Cell(1, 1).Value = $"Laporan Barang Masuk: {from.Value:dd/MM/yyyy} - {to.Value:dd/MM/yyyy}";
            ws.Range("A1:J1").Merge().Style.Font.Bold = true;

            var row = 3;
            ws.Cell(row, 1).Value = "No"; ws.Cell(row, 2).Value = "Tanggal"; ws.Cell(row, 3).Value = "Kode Barang";
            ws.Cell(row, 4).Value = "Nama Barang"; ws.Cell(row, 5).Value = "Kategori"; ws.Cell(row, 6).Value = "Satuan";
            ws.Cell(row, 7).Value = "Jumlah"; ws.Cell(row, 8).Value = "Serial Number";
            ws.Cell(row, 9).Value = "Lokasi"; ws.Cell(row, 10).Value = "Keterangan";
            ws.Range(row, 1, row, 10).Style.Font.Bold = true;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.TanggalMasuk.ToString("dd/MM/yyyy");
                ws.Cell(row, 3).Value = item.Barang?.KodeBarang;
                ws.Cell(row, 4).Value = item.Barang?.NamaBarang;
                ws.Cell(row, 5).Value = item.Barang?.Kategori?.NamaKategori;
                ws.Cell(row, 6).Value = item.Barang?.Satuan;
                ws.Cell(row, 7).Value = item.Jumlah;
                
                string snText = "-";
                if (item.BarangSerials != null && item.BarangSerials.Any())
                {
                    snText = string.Join(", ", item.BarangSerials.Select(s => s.SerialNumber));
                }
                ws.Cell(row, 8).Value = snText;
                
                ws.Cell(row, 9).Value = item.Lokasi?.NamaLokasi ?? "-";
                ws.Cell(row, 10).Value = item.Keterangan;
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
                .Include(bk => bk.BarangSerials)
                .Where(bk => bk.TanggalKeluar >= from && bk.TanggalKeluar <= to.Value.AddDays(1))
                .OrderBy(bk => bk.TanggalKeluar)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Barang Keluar");
            ws.Cell(1, 1).Value = $"Laporan Barang Keluar: {from.Value:dd/MM/yyyy} - {to.Value:dd/MM/yyyy}";
            ws.Range("A1:L1").Merge().Style.Font.Bold = true;

            var row = 3;
            ws.Cell(row, 1).Value = "No"; ws.Cell(row, 2).Value = "Tanggal"; ws.Cell(row, 3).Value = "Kode Barang";
            ws.Cell(row, 4).Value = "Nama Barang"; ws.Cell(row, 5).Value = "Kategori"; ws.Cell(row, 6).Value = "Satuan";
            ws.Cell(row, 7).Value = "Jumlah"; ws.Cell(row, 8).Value = "Serial Number"; ws.Cell(row, 9).Value = "Penerima";
            ws.Cell(row, 10).Value = "PIC"; ws.Cell(row, 11).Value = "No Surat Jalan"; ws.Cell(row, 12).Value = "Keterangan";
            ws.Range(row, 1, row, 12).Style.Font.Bold = true;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.TanggalKeluar.ToString("dd/MM/yyyy");
                ws.Cell(row, 3).Value = item.Barang?.KodeBarang;
                ws.Cell(row, 4).Value = item.Barang?.NamaBarang;
                ws.Cell(row, 5).Value = item.Barang?.Kategori?.NamaKategori;
                ws.Cell(row, 6).Value = item.Barang?.Satuan;
                ws.Cell(row, 7).Value = item.Jumlah;
                
                string snText = "-";
                if (item.BarangSerials != null && item.BarangSerials.Any())
                {
                    snText = string.Join(", ", item.BarangSerials.Select(s => s.SerialNumber));
                }
                ws.Cell(row, 8).Value = snText;
                
                ws.Cell(row, 9).Value = item.Penerima;
                ws.Cell(row, 10).Value = item.Pic;
                ws.Cell(row, 11).Value = item.NoSuratJalan;
                ws.Cell(row, 12).Value = item.Keterangan;
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
                .OrderBy(b => b.NamaBarang)
                .ToListAsync();

            var lokasiData = await _context.BarangLokasis
                .Include(bl => bl.Lokasi)
                .Where(bl => bl.Stok > 0)
                .ToListAsync();
            var lokasiPerBarang = lokasiData
                .GroupBy(bl => bl.BarangId)
                .ToDictionary(g => g.Key, g => g.Select(bl => bl.Lokasi!.NamaLokasi).ToList());

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Stok Barang");
            ws.Cell(1, 1).Value = $"Laporan Stok Barang - {DateTime.Now:dd/MM/yyyy}";
            ws.Range("A1:J1").Merge().Style.Font.Bold = true;

            var row = 3;
            ws.Cell(row, 1).Value = "No"; ws.Cell(row, 2).Value = "Kode"; ws.Cell(row, 3).Value = "Nama Barang";
            ws.Cell(row, 4).Value = "Kategori"; ws.Cell(row, 5).Value = "Satuan";
            ws.Cell(row, 6).Value = "Stok"; ws.Cell(row, 7).Value = "Lokasi"; ws.Cell(row, 8).Value = "Stok Min"; ws.Cell(row, 9).Value = "Status";
            ws.Range(row, 1, row, 9).Style.Font.Bold = true;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.KodeBarang;
                ws.Cell(row, 3).Value = item.NamaBarang;
                ws.Cell(row, 4).Value = item.Kategori?.NamaKategori;
                ws.Cell(row, 5).Value = item.Satuan;
                ws.Cell(row, 6).Value = item.Stok;

                string lokasiText = "-";
                if (lokasiPerBarang.TryGetValue(item.Id, out var lokasiList) && lokasiList.Any())
                {
                    lokasiText = string.Join(", ", lokasiList);
                }
                ws.Cell(row, 7).Value = lokasiText;

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
            ws.Range("A1:M1").Merge().Style.Font.Bold = true;

            var row = 3;
            ws.Cell(row, 1).Value = "No"; ws.Cell(row, 2).Value = "No. Peminjaman"; ws.Cell(row, 3).Value = "Tanggal Pinjam";
            ws.Cell(row, 4).Value = "Kode Barang"; ws.Cell(row, 5).Value = "Nama Barang"; ws.Cell(row, 6).Value = "Kategori";
            ws.Cell(row, 7).Value = "Satuan"; ws.Cell(row, 8).Value = "Serial Number"; ws.Cell(row, 9).Value = "Peminjam"; 
            ws.Cell(row, 10).Value = "Departemen"; ws.Cell(row, 11).Value = "Jatuh Tempo"; ws.Cell(row, 12).Value = "Status"; 
            ws.Cell(row, 13).Value = "Keterangan";
            ws.Range(row, 1, row, 13).Style.Font.Bold = true;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.NoPeminjaman;
                ws.Cell(row, 3).Value = item.TanggalPinjam.ToString("dd/MM/yyyy");
                ws.Cell(row, 4).Value = item.Barang?.KodeBarang;
                ws.Cell(row, 5).Value = item.Barang?.NamaBarang;
                ws.Cell(row, 6).Value = item.Barang?.Kategori?.NamaKategori;
                ws.Cell(row, 7).Value = item.Barang?.Satuan;
                
                string snText = "-";
                // Serials for Peminjaman is not a direct collection. We will set it to '-' or implement later if it is supported.
                ws.Cell(row, 8).Value = snText;
                
                ws.Cell(row, 9).Value = item.Peminjam;
                ws.Cell(row, 10).Value = item.Departemen;
                ws.Cell(row, 11).Value = item.TanggalJatuhTempo.ToString("dd/MM/yyyy");
                ws.Cell(row, 12).Value = item.Status;
                ws.Cell(row, 13).Value = item.Keterangan;

                if (item.Status == "Dipinjam" && item.TanggalJatuhTempo < DateTime.Now)
                    ws.Range(row, 1, row, 13).Style.Fill.BackgroundColor = XLColor.LightSalmon;
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
                .Include(bk => bk.BarangSerials)
                .Where(bk => bk.TanggalKembali >= from && bk.TanggalKembali <= to.Value.AddDays(1))
                .OrderBy(bk => bk.TanggalKembali)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Pengembalian Barang");
            ws.Cell(1, 1).Value = $"Laporan Pengembalian Barang: {from.Value:dd/MM/yyyy} - {to.Value:dd/MM/yyyy}";
            ws.Range("A1:L1").Merge().Style.Font.Bold = true;

            var row = 3;
            ws.Cell(row, 1).Value = "No"; ws.Cell(row, 2).Value = "Tanggal Kembali"; ws.Cell(row, 3).Value = "Kode Barang";
            ws.Cell(row, 4).Value = "Nama Barang"; ws.Cell(row, 5).Value = "Kategori"; ws.Cell(row, 6).Value = "Satuan";
            ws.Cell(row, 7).Value = "Jumlah"; ws.Cell(row, 8).Value = "Serial Number"; ws.Cell(row, 9).Value = "Dikembalikan Oleh";
            ws.Cell(row, 10).Value = "Kondisi"; ws.Cell(row, 11).Value = "Ref. Barang Keluar"; ws.Cell(row, 12).Value = "Keterangan";
            ws.Range(row, 1, row, 12).Style.Font.Bold = true;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.TanggalKembali.ToString("dd/MM/yyyy");
                ws.Cell(row, 3).Value = item.Barang?.KodeBarang;
                ws.Cell(row, 4).Value = item.Barang?.NamaBarang;
                ws.Cell(row, 5).Value = item.Barang?.Kategori?.NamaKategori;
                ws.Cell(row, 6).Value = item.Barang?.Satuan;
                ws.Cell(row, 7).Value = item.Jumlah;
                
                string snText = "-";
                if (item.BarangSerials != null && item.BarangSerials.Any())
                {
                    snText = string.Join(", ", item.BarangSerials.Select(s => s.SerialNumber));
                }
                ws.Cell(row, 8).Value = snText;
                
                ws.Cell(row, 9).Value = item.DikembalikanOleh;
                ws.Cell(row, 10).Value = item.Kondisi;
                ws.Cell(row, 11).Value = item.BarangKeluar?.NoSuratJalan ?? "-";
                ws.Cell(row, 12).Value = item.Keterangan;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Laporan_Pengembalian_{from.Value:yyyyMMdd}_{to.Value:yyyyMMdd}.xlsx");
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
                .Include(t => t.TransferBarangSerials).ThenInclude(tbs => tbs.BarangSerial)
                .Where(t => t.TanggalTransfer >= from && t.TanggalTransfer <= to.Value.AddDays(1))
                .OrderBy(t => t.TanggalTransfer)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Transfer Barang");
            ws.Cell(1, 1).Value = $"Laporan Transfer Ruangan: {from.Value:dd/MM/yyyy} - {to.Value:dd/MM/yyyy}";
            ws.Range("A1:L1").Merge().Style.Font.Bold = true;

            var row = 3;
            ws.Cell(row, 1).Value = "No"; ws.Cell(row, 2).Value = "No. Transfer"; ws.Cell(row, 3).Value = "Tanggal";
            ws.Cell(row, 4).Value = "Kode Barang"; ws.Cell(row, 5).Value = "Nama Barang"; ws.Cell(row, 6).Value = "Kategori";
            ws.Cell(row, 7).Value = "Satuan"; ws.Cell(row, 8).Value = "Jumlah"; ws.Cell(row, 9).Value = "Serial Number";
            ws.Cell(row, 10).Value = "Dari Ruangan"; ws.Cell(row, 11).Value = "Ke Ruangan"; ws.Cell(row, 12).Value = "Keterangan";
            ws.Range(row, 1, row, 12).Style.Font.Bold = true;

            int no = 1;
            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.NoTransfer;
                ws.Cell(row, 3).Value = item.TanggalTransfer.ToString("dd/MM/yyyy");
                ws.Cell(row, 4).Value = item.Barang?.KodeBarang;
                ws.Cell(row, 5).Value = item.Barang?.NamaBarang;
                ws.Cell(row, 6).Value = item.Barang?.Kategori?.NamaKategori;
                ws.Cell(row, 7).Value = item.Barang?.Satuan;
                ws.Cell(row, 8).Value = item.Jumlah;
                
                string snText = "-";
                if (item.TransferBarangSerials != null && item.TransferBarangSerials.Any())
                {
                    snText = string.Join(", ", item.TransferBarangSerials.Select(s => s.BarangSerial?.SerialNumber));
                }
                ws.Cell(row, 9).Value = snText;
                
                ws.Cell(row, 10).Value = item.DariLokasi?.NamaLokasi;
                ws.Cell(row, 11).Value = item.KeLokasi?.NamaLokasi;
                ws.Cell(row, 12).Value = item.Keterangan;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Laporan_TransferRuangan_{from.Value:yyyyMMdd}_{to.Value:yyyyMMdd}.xlsx");
        }
    }
}
