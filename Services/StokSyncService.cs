using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;

namespace itam.Services
{
    public class StokSyncService : IStokSyncService
    {
        private readonly ApplicationDbContext _context;

        public StokSyncService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> SyncBarangAsync(int barangId)
        {
            var barang = await _context.Barangs.FindAsync(barangId);
            if (barang == null) return 0;

            // 1. Hitung total riil transaksi
            var totalMasuk = await _context.BarangMasuks
                .Where(bm => bm.BarangId == barangId)
                .SumAsync(bm => (int?)bm.Jumlah) ?? 0;

            var totalKeluar = await _context.BarangKeluars
                .Where(bk => bk.BarangId == barangId)
                .SumAsync(bk => (int?)bk.Jumlah) ?? 0;

            var activePinjam = await _context.Peminjamans
                .Where(p => p.BarangId == barangId && (p.Status == "Dipinjam" || p.Status == "Terlambat"))
                .SumAsync(p => (int?)p.Jumlah) ?? 0;

            int expectedStok;
            if (totalMasuk > 0 || totalKeluar > 0 || activePinjam > 0)
            {
                expectedStok = Math.Max(0, totalMasuk - totalKeluar - activePinjam);
                barang.Stok = expectedStok;
            }
            else
            {
                // Jika belum ada riwayat transaksi sama sekali (misal master barang baru)
                expectedStok = Math.Max(0, barang.Stok);
                barang.Stok = expectedStok;
            }

            // 2. Sinkronisasi Unit Serial Numbers (BarangSerials)
            var serials = await _context.BarangSerials
                .Where(s => s.BarangId == barangId)
                .ToListAsync();

            var keluarIds = await _context.BarangKeluars
                .Where(bk => bk.BarangId == barangId)
                .Select(bk => bk.Id)
                .ToListAsync();

            var pinjamSnIds = await _context.Peminjamans
                .Where(p => p.BarangId == barangId && (p.Status == "Dipinjam" || p.Status == "Terlambat") && p.BarangSerialId.HasValue)
                .Select(p => p.BarangSerialId!.Value)
                .ToListAsync();

            var kembaliMap = await _context.BarangKembalis
                .Where(bk => bk.BarangId == barangId)
                .ToDictionaryAsync(bk => bk.Id, bk => bk.TindakLanjut);

            // Step 2A: Setel status berdasarkan referensi transaksi
            foreach (var s in serials)
            {
                if (pinjamSnIds.Contains(s.Id))
                {
                    s.Status = "Dipinjam";
                }
                else if (s.BarangKembaliId.HasValue && kembaliMap.TryGetValue(s.BarangKembaliId.Value, out var tindakLanjut))
                {
                    if (tindakLanjut == "Di Disposal")
                        s.Status = "Disposal";
                    else if (tindakLanjut == "Diperbaiki")
                        s.Status = "Diperbaiki";
                    else
                        s.Status = "Tersedia";
                }
                else if (s.BarangKeluarId.HasValue && keluarIds.Contains(s.BarangKeluarId.Value))
                {
                    s.Status = "Keluar";
                }
                else
                {
                    if (s.Status != "Disposal" && s.Status != "Diperbaiki")
                    {
                        s.Status = "Tersedia";
                    }
                }
            }

            // Step 2B: Selaraskan jumlah SN berstatus "Tersedia" dengan expectedStok
            var tersediaSerials = serials.Where(s => s.Status == "Tersedia").ToList();
            if (tersediaSerials.Count > expectedStok)
            {
                // Kelebihan SN "Tersedia" -> Tandai yang tertua / placeholder "-" sebagai "Keluar"
                int excess = tersediaSerials.Count - expectedStok;
                var toMarkKeluar = tersediaSerials
                    .OrderBy(s => s.SerialNumber == "-" ? 0 : 1)
                    .ThenBy(s => s.Id)
                    .Take(excess)
                    .ToList();

                foreach (var s in toMarkKeluar)
                {
                    s.Status = "Keluar";
                }
            }
            else if (tersediaSerials.Count < expectedStok)
            {
                int deficit = expectedStok - tersediaSerials.Count;

                // Kembalikan SN "Keluar" yang tidak terkait transaksi resmi keluar jika ada
                var unlinkedKeluar = serials
                    .Where(s => s.Status == "Keluar" && !s.BarangKeluarId.HasValue)
                    .Take(deficit)
                    .ToList();

                foreach (var s in unlinkedKeluar)
                {
                    s.Status = "Tersedia";
                    deficit--;
                }

                // Jika masih kurang (misal barang non-SN), buat placeholder "-"
                for (int i = 0; i < deficit; i++)
                {
                    _context.BarangSerials.Add(new BarangSerial
                    {
                        BarangId = barangId,
                        SerialNumber = "-",
                        Kondisi = "Baru",
                        Status = "Tersedia",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // 3. Sinkronisasi Stok Lokasi (BarangLokasi)
            var bls = await _context.BarangLokasis
                .Where(bl => bl.BarangId == barangId)
                .ToListAsync();

            int totalLokasiStok = bls.Sum(x => x.Stok);
            if (totalLokasiStok != expectedStok)
            {
                if (bls.Count == 1)
                {
                    bls[0].Stok = expectedStok;
                }
                else if (bls.Count > 1)
                {
                    int diff = expectedStok - totalLokasiStok;
                    var mainLok = bls.OrderByDescending(x => x.Stok).First();
                    mainLok.Stok = Math.Max(0, mainLok.Stok + diff);
                }
                else if (expectedStok > 0)
                {
                    var defaultLok = await _context.Lokasis.FirstOrDefaultAsync(l => l.NamaLokasi == "Ruang IT")
                                  ?? await _context.Lokasis.FirstOrDefaultAsync();
                    if (defaultLok != null)
                    {
                        _context.BarangLokasis.Add(new BarangLokasi
                        {
                            BarangId = barangId,
                            LokasiId = defaultLok.Id,
                            Stok = expectedStok
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return expectedStok;
        }

        public async Task<int> SyncAllBarangAsync()
        {
            var barangIds = await _context.Barangs.Select(b => b.Id).ToListAsync();
            int count = 0;
            foreach (var bId in barangIds)
            {
                await SyncBarangAsync(bId);
                count++;
            }
            return count;
        }
    }
}
