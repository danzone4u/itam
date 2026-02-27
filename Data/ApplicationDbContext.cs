using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyGudang.Models;

namespace MyGudang.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Kategori> Kategoris { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Barang> Barangs { get; set; }
        public DbSet<BarangMasuk> BarangMasuks { get; set; }
        public DbSet<BarangKeluar> BarangKeluars { get; set; }
        public DbSet<StokOpname> StokOpnames { get; set; }
        public DbSet<StokOpnameDetail> StokOpnameDetails { get; set; }
        public DbSet<Arsip> Arsips { get; set; }
        public DbSet<ChartSetting> ChartSettings { get; set; }
        public DbSet<KopSurat> KopSurats { get; set; }
        public DbSet<BarangKembali> BarangKembalis { get; set; }
        public DbSet<BarangSerial> BarangSerials { get; set; }
        public DbSet<SuratSetting> SuratSettings { get; set; }
        public DbSet<Peminjaman> Peminjamans { get; set; }
        public DbSet<Lokasi> Lokasis { get; set; }
        public DbSet<BarangLokasi> BarangLokasis { get; set; }
        public DbSet<TransferBarang> TransferBarangs { get; set; }
        public DbSet<BackupSetting> BackupSettings { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Peremajaan> Peremajaans { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<Barang>()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                entityEntry.Entity.UpdatedAt = DateTime.Now;
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Barang>()
                .HasOne(b => b.Kategori)
                .WithMany(k => k.Barangs)
                .HasForeignKey(b => b.KategoriId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Barang>()
                .HasOne(b => b.Supplier)
                .WithMany(s => s.Barangs)
                .HasForeignKey(b => b.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BarangMasuk>()
                .HasOne(bm => bm.Barang)
                .WithMany(b => b.BarangMasuks)
                .HasForeignKey(bm => bm.BarangId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BarangKeluar>()
                .HasOne(bk => bk.Barang)
                .WithMany(b => b.BarangKeluars)
                .HasForeignKey(bk => bk.BarangId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StokOpnameDetail>()
                .HasOne(d => d.StokOpname)
                .WithMany(s => s.Details)
                .HasForeignKey(d => d.StokOpnameId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StokOpnameDetail>()
                .HasOne(d => d.Barang)
                .WithMany()
                .HasForeignKey(d => d.BarangId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BarangKembali>()
                .HasOne(bk => bk.Barang)
                .WithMany(b => b.BarangKembalis)
                .HasForeignKey(bk => bk.BarangId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BarangKembali>()
                .HasOne(bk => bk.BarangKeluar)
                .WithMany()
                .HasForeignKey(bk => bk.BarangKeluarId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BarangSerial>()
                .HasOne(bs => bs.Barang)
                .WithMany(b => b.BarangSerials)
                .HasForeignKey(bs => bs.BarangId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BarangSerial>()
                .HasOne(bs => bs.BarangMasuk)
                .WithMany()
                .HasForeignKey(bs => bs.BarangMasukId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BarangSerial>()
                .HasOne(bs => bs.BarangKeluar)
                .WithMany(bk => bk.BarangSerials)
                .HasForeignKey(bs => bs.BarangKeluarId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BarangLokasi>()
                .HasOne(bl => bl.Barang)
                .WithMany()
                .HasForeignKey(bl => bl.BarangId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BarangLokasi>()
                .HasOne(bl => bl.Lokasi)
                .WithMany(l => l.BarangLokasis)
                .HasForeignKey(bl => bl.LokasiId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TransferBarang>()
                .HasOne(t => t.Barang)
                .WithMany()
                .HasForeignKey(t => t.BarangId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TransferBarang>()
                .HasOne(t => t.DariLokasi)
                .WithMany()
                .HasForeignKey(t => t.DariLokasiId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TransferBarang>()
                .HasOne(t => t.KeLokasi)
                .WithMany()
                .HasForeignKey(t => t.KeLokasiId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BarangMasuk>()
                .HasOne(bm => bm.Lokasi)
                .WithMany()
                .HasForeignKey(bm => bm.LokasiId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BarangKeluar>()
                .HasOne(bk => bk.Lokasi)
                .WithMany()
                .HasForeignKey(bk => bk.LokasiId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
