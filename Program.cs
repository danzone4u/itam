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
            new() { NamaKategori = "Komputer & Laptop", Deskripsi = "Perangkat komputasi end-user" },
            new() { NamaKategori = "Server & Storage", Deskripsi = "Server fisik, NAS, dan perangkat penyimpanan" },
            new() { NamaKategori = "Jaringan", Deskripsi = "Switch, Router, Access Point, Kabel" },
            new() { NamaKategori = "Peripheral", Deskripsi = "Monitor, Printer, Scanner" },
            new() { NamaKategori = "Aksesoris IT", Deskripsi = "Keyboard, Mouse, Headset, UPS" }
        };
        context.Kategoris.AddRange(kategoris);
        await context.SaveChangesAsync();

        var suppliers = new List<Supplier>
        {
            new() { NamaSupplier = "PT Integra Teknologi", Alamat = "Jl. Sudirman 10, Jakarta", Telepon = "021-5551000", Email = "sales@integra.co.id" },
            new() { NamaSupplier = "CV Network Solusindo", Alamat = "Jl. Pemuda 45, Surabaya", Telepon = "031-4442000", Email = "info@netsolindo.com" },
            new() { NamaSupplier = "Bhinneka Enterprise", Alamat = "Jl. Gunung Sahari, Jakarta", Telepon = "021-3333000", Email = "b2b@bhinneka.com" },
            new() { NamaSupplier = "PT Surya Server", Alamat = "Jl. Gatot Subroto 56, Bandung", Telepon = "022-6664000", Email = "contact@suryaserver.id" }
        };
        context.Suppliers.AddRange(suppliers);
        await context.SaveChangesAsync();

        var k = await context.Kategoris.ToListAsync();
        var s = await context.Suppliers.ToListAsync();

        var barangs = new List<Barang>
        {
            new() { KodeBarang = "PC-001", NamaBarang = "Lenovo ThinkPad T14 Gen 3", KategoriId = k[0].Id, SupplierId = s[0].Id, Satuan = "Unit", Stok = 25 },
            new() { KodeBarang = "PC-002", NamaBarang = "Dell OptiPlex 7000 SFF", KategoriId = k[0].Id, SupplierId = s[2].Id, Satuan = "Unit", Stok = 15 },
            new() { KodeBarang = "PC-003", NamaBarang = "MacBook Pro 14 M2", KategoriId = k[0].Id, SupplierId = s[2].Id, Satuan = "Unit", Stok = 5 },
            new() { KodeBarang = "SRV-001", NamaBarang = "Dell PowerEdge R750", KategoriId = k[1].Id, SupplierId = s[3].Id, Satuan = "Unit", Stok = 2 },
            new() { KodeBarang = "SRV-002", NamaBarang = "Synology NAS RackStation RS1221+", KategoriId = k[1].Id, SupplierId = s[3].Id, Satuan = "Unit", Stok = 3 },
            new() { KodeBarang = "SRV-003", NamaBarang = "HDD WD Red Pro 10TB", KategoriId = k[1].Id, SupplierId = s[3].Id, Satuan = "Pcs", Stok = 10 },
            new() { KodeBarang = "NET-001", NamaBarang = "Switch Cisco Catalyst 9200L 48-port", KategoriId = k[2].Id, SupplierId = s[1].Id, Satuan = "Unit", Stok = 4 },
            new() { KodeBarang = "NET-002", NamaBarang = "MikroTik Cloud Core Router CCR2004", KategoriId = k[2].Id, SupplierId = s[1].Id, Satuan = "Unit", Stok = 3 },
            new() { KodeBarang = "NET-003", NamaBarang = "Access Point Ubiquiti U6-Pro", KategoriId = k[2].Id, SupplierId = s[1].Id, Satuan = "Unit", Stok = 12 },
            new() { KodeBarang = "NET-004", NamaBarang = "Kabel UTP Belden Cat6 305m", KategoriId = k[2].Id, SupplierId = s[1].Id, Satuan = "Roll", Stok = 8 },
            new() { KodeBarang = "PRP-001", NamaBarang = "Monitor Dell UltraSharp 27 U2722D", KategoriId = k[3].Id, SupplierId = s[0].Id, Satuan = "Unit", Stok = 20 },
            new() { KodeBarang = "PRP-002", NamaBarang = "Printer HP Color LaserJet Pro M454dn", KategoriId = k[3].Id, SupplierId = s[2].Id, Satuan = "Unit", Stok = 6 },
            new() { KodeBarang = "PRP-003", NamaBarang = "Scanner Epson WorkForce DS-530 II", KategoriId = k[3].Id, SupplierId = s[2].Id, Satuan = "Unit", Stok = 4 },
            new() { KodeBarang = "AKS-001", NamaBarang = "Keyboard Logitech MX Keys", KategoriId = k[4].Id, SupplierId = s[0].Id, Satuan = "Pcs", Stok = 15 },
            new() { KodeBarang = "AKS-002", NamaBarang = "Mouse Logitech MX Master 3S", KategoriId = k[4].Id, SupplierId = s[0].Id, Satuan = "Pcs", Stok = 15 },
            new() { KodeBarang = "AKS-003", NamaBarang = "Headset Jabra Evolve2 65", KategoriId = k[4].Id, SupplierId = s[2].Id, Satuan = "Pcs", Stok = 10 },
            new() { KodeBarang = "AKS-004", NamaBarang = "UPS APC Smart-UPS 1500VA", KategoriId = k[4].Id, SupplierId = s[3].Id, Satuan = "Unit", Stok = 8 }
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
                Keterangan = "Pengadaan awal perangkat IT",
                HargaSatuan = rng.Next(5, 150) * 100000 // Random price between 500k and 15M
            });
        }
        await context.SaveChangesAsync();

        // Seed default charts
        context.ChartSettings.AddRange(
            new ChartSetting { NamaChart = "Barang Masuk vs Keluar", TipeChart = "bar", SumberData = "masuk_keluar", JumlahBulan = 6, WarnaUtama = "#28a745", WarnaKedua = "#dc3545", Lebar = 8, Urutan = 1, Aktif = true },
            new ChartSetting { NamaChart = "Aset IT per Kategori", TipeChart = "doughnut", SumberData = "per_kategori", Lebar = 4, Urutan = 2, Aktif = true }
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
