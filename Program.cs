using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyGudang.Data;
using MyGudang.Models;
using MyGudang.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseCompatibilityLevel(120)));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddHostedService<BackupService>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
});

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();

    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Seed roles
    string[] roles = { "SuperAdmin", "AdminGudang" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    if (!userManager.Users.Any())
    {
        var admin = new IdentityUser
        {
            UserName = "admin",
            Email = "admin@mygudang.com",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(admin, "Admin@123");
        await userManager.AddToRoleAsync(admin, "SuperAdmin");
    }
    else
    {
        // Ensure existing admin has SuperAdmin role
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
        {
            await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
        }
    }

    // Seed dummy data
    if (!context.Kategoris.Any())
    {
        var kategoris = new List<Kategori>
        {
            new() { NamaKategori = "Elektronik", Deskripsi = "Perangkat elektronik dan komputer" },
            new() { NamaKategori = "Furniture", Deskripsi = "Meja, kursi, dan perabot kantor" },
            new() { NamaKategori = "ATK", Deskripsi = "Alat tulis kantor" },
            new() { NamaKategori = "Jaringan", Deskripsi = "Perangkat jaringan dan kabel" },
            new() { NamaKategori = "Aksesoris", Deskripsi = "Aksesoris komputer dan gadget" }
        };
        context.Kategoris.AddRange(kategoris);
        await context.SaveChangesAsync();

        var suppliers = new List<Supplier>
        {
            new() { NamaSupplier = "PT Maju Jaya Teknologi", Alamat = "Jl. Sudirman No. 123, Jakarta", Telepon = "021-5551234", Email = "info@majujaya.co.id" },
            new() { NamaSupplier = "CV Sumber Makmur", Alamat = "Jl. Ahmad Yani No. 45, Surabaya", Telepon = "031-4445678", Email = "order@sumbermakmur.com" },
            new() { NamaSupplier = "UD Berkah Sentosa", Alamat = "Jl. Diponegoro No. 78, Bandung", Telepon = "022-3334567", Email = "sales@berkahsentosa.id" },
            new() { NamaSupplier = "PT Global Komputer", Alamat = "Jl. Gatot Subroto No. 56, Semarang", Telepon = "024-6667890", Email = "cs@globalkom.co.id" }
        };
        context.Suppliers.AddRange(suppliers);
        await context.SaveChangesAsync();

        var k = await context.Kategoris.ToListAsync();
        var s = await context.Suppliers.ToListAsync();

        var barangs = new List<Barang>
        {
            new() { KodeBarang = "ELK-001", NamaBarang = "Laptop ASUS VivoBook 14", KategoriId = k[0].Id, SupplierId = s[0].Id, Satuan = "Unit", Stok = 15 },
            new() { KodeBarang = "ELK-002", NamaBarang = "Monitor LED 24 inch", KategoriId = k[0].Id, SupplierId = s[3].Id, Satuan = "Unit", Stok = 10 },
            new() { KodeBarang = "ELK-003", NamaBarang = "Printer HP LaserJet Pro", KategoriId = k[0].Id, SupplierId = s[0].Id, Satuan = "Unit", Stok = 8 },
            new() { KodeBarang = "ELK-004", NamaBarang = "UPS APC 1200VA", KategoriId = k[0].Id, SupplierId = s[3].Id, Satuan = "Unit", Stok = 12 },
            new() { KodeBarang = "FRN-001", NamaBarang = "Meja Kerja 120x60 cm", KategoriId = k[1].Id, SupplierId = s[1].Id, Satuan = "Unit", Stok = 20 },
            new() { KodeBarang = "FRN-002", NamaBarang = "Kursi Kantor Ergonomis", KategoriId = k[1].Id, SupplierId = s[1].Id, Satuan = "Unit", Stok = 25 },
            new() { KodeBarang = "FRN-003", NamaBarang = "Lemari Arsip 4 Laci", KategoriId = k[1].Id, SupplierId = s[1].Id, Satuan = "Unit", Stok = 6 },
            new() { KodeBarang = "FRN-004", NamaBarang = "Rak Buku Besi 5 Tingkat", KategoriId = k[1].Id, SupplierId = s[2].Id, Satuan = "Unit", Stok = 4 },
            new() { KodeBarang = "ATK-001", NamaBarang = "Kertas HVS A4 80gr", KategoriId = k[2].Id, SupplierId = s[2].Id, Satuan = "Rim", Stok = 50 },
            new() { KodeBarang = "ATK-002", NamaBarang = "Toner HP 05A", KategoriId = k[2].Id, SupplierId = s[0].Id, Satuan = "Pcs", Stok = 3 },
            new() { KodeBarang = "ATK-003", NamaBarang = "Pulpen Pilot G2", KategoriId = k[2].Id, SupplierId = s[2].Id, Satuan = "Lusin", Stok = 30 },
            new() { KodeBarang = "ATK-004", NamaBarang = "Map Ordner", KategoriId = k[2].Id, SupplierId = s[2].Id, Satuan = "Pcs", Stok = 40 },
            new() { KodeBarang = "ATK-005", NamaBarang = "Stapler Besar HD-12", KategoriId = k[2].Id, SupplierId = s[2].Id, Satuan = "Pcs", Stok = 10 },
            new() { KodeBarang = "JRG-001", NamaBarang = "Switch Managed 24 Port", KategoriId = k[3].Id, SupplierId = s[3].Id, Satuan = "Unit", Stok = 5 },
            new() { KodeBarang = "JRG-002", NamaBarang = "Router Mikrotik RB750Gr3", KategoriId = k[3].Id, SupplierId = s[3].Id, Satuan = "Unit", Stok = 7 },
            new() { KodeBarang = "JRG-003", NamaBarang = "Kabel UTP Cat6 305m", KategoriId = k[3].Id, SupplierId = s[3].Id, Satuan = "Box", Stok = 3 },
            new() { KodeBarang = "JRG-004", NamaBarang = "Access Point UniFi AC Lite", KategoriId = k[3].Id, SupplierId = s[3].Id, Satuan = "Unit", Stok = 2 },
            new() { KodeBarang = "AKS-001", NamaBarang = "Mouse Wireless Logitech", KategoriId = k[4].Id, SupplierId = s[0].Id, Satuan = "Pcs", Stok = 20 },
            new() { KodeBarang = "AKS-002", NamaBarang = "Keyboard Mechanical", KategoriId = k[4].Id, SupplierId = s[0].Id, Satuan = "Pcs", Stok = 15 },
            new() { KodeBarang = "AKS-003", NamaBarang = "Headset USB Jabra", KategoriId = k[4].Id, SupplierId = s[0].Id, Satuan = "Pcs", Stok = 0 }
        };
        context.Barangs.AddRange(barangs);
        await context.SaveChangesAsync();

        // Seed barang masuk history
        var allBarang = await context.Barangs.ToListAsync();
        var rng = new Random(42);
        foreach (var b in allBarang.Where(b => b.Stok > 0))
        {
            context.BarangMasuks.Add(new BarangMasuk
            {
                BarangId = b.Id,
                Jumlah = b.Stok,
                TanggalMasuk = DateTime.Now.AddDays(-rng.Next(7, 60)),
                Keterangan = "Stok awal"
            });
        }
        await context.SaveChangesAsync();

        // Seed default charts
        context.ChartSettings.AddRange(
            new ChartSetting { NamaChart = "Barang Masuk vs Keluar", TipeChart = "bar", SumberData = "masuk_keluar", JumlahBulan = 6, WarnaUtama = "#28a745", WarnaKedua = "#dc3545", Lebar = 8, Urutan = 1, Aktif = true },
            new ChartSetting { NamaChart = "Barang per Kategori", TipeChart = "doughnut", SumberData = "per_kategori", Lebar = 4, Urutan = 2, Aktif = true }
        );
        await context.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
