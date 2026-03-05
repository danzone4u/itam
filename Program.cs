using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using itam.Data;
using itam.Models;
using itam.Services;

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
    if (!context.Barangs.Any(b => b.NamaBarang == "PC Server Rack mount A1"))
    {
        // Delete all old data from deepest to shallowest to avoid FK conflicts and stale model errors
        await context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM BarangKembalis;
            DELETE FROM TransferBarangSerials;
            DELETE FROM TransferBarangs;
            DELETE FROM StokOpnameDetails;
            DELETE FROM StokOpnames;
            DELETE FROM BarangLokasis;
            DELETE FROM BarangSerials;
            DELETE FROM BarangKeluars;
            DELETE FROM BarangMasuks;
            DELETE FROM Peminjamans;
            DELETE FROM Barangs;
            DELETE FROM Kategoris;
            DELETE FROM Suppliers;
            DELETE FROM Arsips;
        ");

        var kategoris = new List<Kategori>
        {
            new() { NamaKategori = "Komputer & Laptop", Deskripsi = "Perangkat komputasi end-user" },
            new() { NamaKategori = "Server & Data Center", Deskripsi = "Server fisik, NAS, dan komputasi intensif" },
            new() { NamaKategori = "Jaringan & Keamanan", Deskripsi = "Switch, Router, Firewall, Access Point, Kamera CCTV" },
            new() { NamaKategori = "Perangkat Pendukung", Deskripsi = "Monitor, Printer, Scanner, Proyektor" },
            new() { NamaKategori = "Aksesoris IT & Suku Cadang", Deskripsi = "Keyboard, Mouse, Headset, UPS, Kabel, RAM, SSD" }
        };
        context.Kategoris.AddRange(kategoris);
        await context.SaveChangesAsync();

        var suppliers = new List<Supplier>
        {
            new() { NamaSupplier = "PT Integra Teknologi Global", Alamat = "Gd. Cyber Lt. 5, Jl. Kuningan Barat No. 8, Jakarta Selatan", Telepon = "021-5551000", Email = "sales@integra.co.id" },
            new() { NamaSupplier = "CV Network Solusindo Utama", Alamat = "Jl. Raya Jemursari No. 120, Surabaya", Telepon = "031-4442000", Email = "info@netsolindo.com" },
            new() { NamaSupplier = "Bhinneka Enterprise Solutions", Alamat = "Jl. Gunung Sahari Raya No. 73C, Jakarta Pusat", Telepon = "021-3333000", Email = "b2b@bhinneka.com" },
            new() { NamaSupplier = "PT Surya Server Indonesia", Alamat = "Kawasan Industri Tekno, Gedebage, Bandung", Telepon = "022-6664000", Email = "contact@suryaserver.id" }
        };
        context.Suppliers.AddRange(suppliers);
        await context.SaveChangesAsync();

        var k = await context.Kategoris.ToListAsync();
        var s = await context.Suppliers.ToListAsync();

        var barangs = new List<Barang>
        {
            // Komputer & Laptop
            new() { KodeBarang = "PC-LP-01", NamaBarang = "Lenovo ThinkPad T14 Gen 4 (Core i7, 16GB, 512GB SSD)", KategoriId = k[0].Id, Satuan = "Unit", Stok = 20 },
            new() { KodeBarang = "PC-LP-02", NamaBarang = "Dell Latitude 7440 (Core i5, 16GB, 512GB SSD)", KategoriId = k[0].Id, Satuan = "Unit", Stok = 15 },
            new() { KodeBarang = "PC-LP-03", NamaBarang = "MacBook Pro 14 M3 Pro (18GB, 512GB SSD)", KategoriId = k[0].Id, Satuan = "Unit", Stok = 5 },
            new() { KodeBarang = "PC-DT-01", NamaBarang = "Dell OptiPlex 7010 Micro (Core i5, 8GB, 256GB SSD)", KategoriId = k[0].Id, Satuan = "Unit", Stok = 30 },
            new() { KodeBarang = "PC-DT-02", NamaBarang = "HP Pro Mini 400 G9 (Core i7, 16GB, 512GB SSD)", KategoriId = k[0].Id, Satuan = "Unit", Stok = 10 },
            new() { KodeBarang = "PC-DT-WS", NamaBarang = "Lenovo ThinkStation P360 Tower (Xeon, 32GB, RTX 4000)", KategoriId = k[0].Id, Satuan = "Unit", Stok = 2 },
            
            // Server & Data Center
            new() { KodeBarang = "SRV-RK-01", NamaBarang = "Dell PowerEdge R750 (2x Intel Xeon Gold, 128GB RAM)", KategoriId = k[1].Id, Satuan = "Unit", Stok = 3 },
            new() { KodeBarang = "SRV-RK-02", NamaBarang = "HPE ProLiant DL380 Gen11 (2x Intel Xeon Silver, 64GB RAM)", KategoriId = k[1].Id, Satuan = "Unit", Stok = 2 },
            new() { KodeBarang = "SRV-NS-01", NamaBarang = "Synology RackStation RS1221+ (8-bay, 4GB RAM)", KategoriId = k[1].Id, Satuan = "Unit", Stok = 1 },
            new() { KodeBarang = "SRV-NS-02", NamaBarang = "QNAP TS-464 (4-bay Desktop NAS)", KategoriId = k[1].Id, Satuan = "Unit", Stok = 2 },
            new() { KodeBarang = "SRV-HD-01", NamaBarang = "HDD Enterprise WD Gold 12TB", KategoriId = k[1].Id, Satuan = "Pcs", Stok = 16 },
            new() { KodeBarang = "SRV-HD-02", NamaBarang = "SSD Enterprise Samsung PM893 1.92TB", KategoriId = k[1].Id, Satuan = "Pcs", Stok = 8 },
            
            // Jaringan & Keamanan
            new() { KodeBarang = "NET-SW-01", NamaBarang = "Cisco Catalyst 9200L 48-port PoE+", KategoriId = k[2].Id, Satuan = "Unit", Stok = 4 },
            new() { KodeBarang = "NET-SW-02", NamaBarang = "Aruba Instant On 1930 24-port Gb Smart Switch", KategoriId = k[2].Id, Satuan = "Unit", Stok = 8 },
            new() { KodeBarang = "NET-RT-01", NamaBarang = "MikroTik Cloud Core Router CCR2004-16G-2S+", KategoriId = k[2].Id, Satuan = "Unit", Stok = 2 },
            new() { KodeBarang = "NET-FW-01", NamaBarang = "Fortinet FortiGate 60F", KategoriId = k[2].Id, Satuan = "Unit", Stok = 3 },
            new() { KodeBarang = "NET-AP-01", NamaBarang = "Ubiquiti UniFi6 Pro (U6-Pro)", KategoriId = k[2].Id, Satuan = "Unit", Stok = 15 },
            new() { KodeBarang = "NET-AP-02", NamaBarang = "Ruckus R350 Indoor Access Point", KategoriId = k[2].Id, Satuan = "Unit", Stok = 5 },
            new() { KodeBarang = "NET-CBL-01", NamaBarang = "Kabel UTP Belden Cat6 305m (Indoor)", KategoriId = k[2].Id, Satuan = "Roll", Stok = 10 },
            new() { KodeBarang = "SEC-CAM-01", NamaBarang = "Hikvision IP Camera Dome 4MP", KategoriId = k[2].Id, Satuan = "Unit", Stok = 12 },
            
            // Perangkat Pendukung
            new() { KodeBarang = "PRP-MN-01", NamaBarang = "Monitor Dell UltraSharp 27\" U2722D", KategoriId = k[3].Id, Satuan = "Unit", Stok = 25 },
            new() { KodeBarang = "PRP-MN-02", NamaBarang = "Monitor Lenovo ThinkVision 24\" T24i-30", KategoriId = k[3].Id, Satuan = "Unit", Stok = 40 },
            new() { KodeBarang = "PRP-PR-01", NamaBarang = "Printer HP Color LaserJet Pro M454dn", KategoriId = k[3].Id, Satuan = "Unit", Stok = 5 },
            new() { KodeBarang = "PRP-PR-02", NamaBarang = "Printer Brother HL-L2365DW (Monochrome)", KategoriId = k[3].Id, Satuan = "Unit", Stok = 8 },
            new() { KodeBarang = "PRP-SC-01", NamaBarang = "Scanner Epson WorkForce DS-530 II", KategoriId = k[3].Id, Satuan = "Unit", Stok = 3 },
            new() { KodeBarang = "PRP-PJ-01", NamaBarang = "Proyektor Epson EB-X51 XGA", KategoriId = k[3].Id, Satuan = "Unit", Stok = 4 },
            
            // Aksesoris IT & Suku Cadang
            new() { KodeBarang = "AKS-KB-01", NamaBarang = "Keyboard Wireless Logitech MX Keys", KategoriId = k[4].Id, Satuan = "Pcs", Stok = 15 },
            new() { KodeBarang = "AKS-MS-01", NamaBarang = "Mouse Wireless Logitech MX Master 3S", KategoriId = k[4].Id, Satuan = "Pcs", Stok = 15 },
            new() { KodeBarang = "AKS-CBO-01", NamaBarang = "Combo Keyboard Mouse Logitech MK240", KategoriId = k[4].Id, Satuan = "Set", Stok = 30 },
            new() { KodeBarang = "AKS-HS-01", NamaBarang = "Headset Jabra Evolve2 65", KategoriId = k[4].Id, Satuan = "Pcs", Stok = 10 },
            new() { KodeBarang = "AKS-HS-02", NamaBarang = "Headset Plantronics Blackwire 3220", KategoriId = k[4].Id, Satuan = "Pcs", Stok = 20 },
            new() { KodeBarang = "AKS-UP-01", NamaBarang = "UPS APC Smart-UPS 1500VA LCD", KategoriId = k[4].Id, Satuan = "Unit", Stok = 5 },
            new() { KodeBarang = "AKS-UP-02", NamaBarang = "UPS Prolink PRO700SFC 650VA", KategoriId = k[4].Id, Satuan = "Unit", Stok = 15 },
            new() { KodeBarang = "AKS-RAM-01", NamaBarang = "RAM DDR4 Kingston FURY 16GB 3200MHz", KategoriId = k[4].Id, Satuan = "Pcs", Stok = 20 },
            new() { KodeBarang = "AKS-SSD-01", NamaBarang = "SSD Samsung 980 PRO NVMe M.2 1TB", KategoriId = k[4].Id, Satuan = "Pcs", Stok = 15 },
            new() { KodeBarang = "AKS-FD-01", NamaBarang = "Flashdisk SanDisk Ultra Dual Drive Luxe 64GB", KategoriId = k[4].Id, Satuan = "Pcs", Stok = 50 }
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
                SupplierId = s[rng.Next(s.Count)].Id,
                Jumlah = b.Stok,
                TanggalMasuk = DateTime.Now.AddDays(-rng.Next(7, 30)),
                Keterangan = "Pengadaan Inventaris IT Q1 2026",
                HargaSatuan = b.KodeBarang.Contains("LP") ? 20000000 : 
                              b.KodeBarang.Contains("SRV-RK") ? 80000000 :
                              b.KodeBarang.Contains("NET-SW-01") ? 45000000 :
                              rng.Next(5, 50) * 100000
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
