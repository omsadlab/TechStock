using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TechStock.Domain.Entities;
using TechStock.Domain.Enums;

namespace TechStock.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await db.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedProductTypesAndConfigsAsync(db);
        await SeedBrandsAsync(db);
        await SeedAdminUserAsync(userManager);
        await SeedAppSettingsAsync(db);

        await db.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in new[] { "Admin", "Manager", "Salesperson" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    private static async Task SeedProductTypesAndConfigsAsync(AppDbContext db)
    {
        var data = new Dictionary<string, string[]>
        {
            ["Laptop"]       = ["CPU", "RAM", "Storage", "Display Size", "Display Type", "GPU", "OS", "Color", "Weight", "Battery Life"],
            ["Desktop"]      = ["CPU", "Motherboard", "RAM", "Storage", "GPU", "PSU", "Case", "Cooler", "OS"],
            ["RAM"]          = ["Capacity", "Type", "Speed (MHz)", "Form Factor"],
            ["VGA"]          = ["Chip", "VRAM", "Memory Type", "Boost Clock", "TDP"],
            ["SSD"]          = ["Capacity", "Interface", "Read Speed", "Write Speed"],
            ["HDD"]          = ["Capacity", "RPM", "Interface", "Cache"],
            ["CPU"]          = ["Cores", "Threads", "Base Clock", "Boost Clock", "Socket", "TDP", "Cache"],
            ["Motherboard"]  = ["Socket", "Chipset", "Form Factor", "RAM Slots", "Max RAM", "PCIe Slots"],
            ["PSU"]          = ["Wattage", "Rating", "Modular"],
            ["Display"]      = ["Size", "Resolution", "Panel Type", "Refresh Rate", "Response Time", "Ports"],
            ["Keyboard"]     = ["Type", "Switch", "Connectivity", "Backlight"],
            ["Mouse"]        = ["DPI Range", "Buttons", "Connectivity", "Weight"],
            ["Mobile"]       = ["CPU", "RAM", "Storage", "Display", "Battery", "Camera", "OS", "Color"],
            ["Other"]        = ["Description"],
        };

        foreach (var (typeName, configs) in data)
        {
            var pt = await db.ProductTypes.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Name == typeName);

            if (pt == null)
            {
                pt = new ProductType { Name = typeName };
                db.ProductTypes.Add(pt);
                await db.SaveChangesAsync();
            }

            for (int i = 0; i < configs.Length; i++)
            {
                var configName = configs[i];
                if (!await db.ConfigTypes.AnyAsync(c => c.ProductTypeId == pt.Id && c.Name == configName))
                {
                    db.ConfigTypes.Add(new ConfigType
                    {
                        ProductTypeId = pt.Id,
                        Name = configName,
                        DisplayOrder = i,
                    });
                }
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedBrandsAsync(AppDbContext db)
    {
        var brands = new[]
        {
            "Dell", "ASUS", "HP", "Lenovo", "Apple", "MSI", "Gigabyte", "Samsung",
            "Kingston", "Corsair", "G.Skill", "Western Digital", "Seagate", "LG",
            "BenQ", "Acer", "Razer", "Logitech", "Intel", "AMD", "NVIDIA",
            "Cooler Master", "Noctua"
        };

        foreach (var name in brands)
        {
            if (!await db.Brands.IgnoreQueryFilters().AnyAsync(b => b.Name == name))
                db.Brands.Add(new Brand { Name = name });
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<AppUser> userManager)
    {
        const string email = "admin@techstock.lk";
        if (await userManager.FindByEmailAsync(email) != null) return;

        var admin = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = "System Admin",
            Role = UserRole.Admin,
            IsActive = true,
            EmailConfirmed = true,
        };

        await userManager.CreateAsync(admin, "Admin@123!");
        await userManager.AddToRoleAsync(admin, "Admin");
    }

    private static async Task SeedAppSettingsAsync(AppDbContext db)
    {
        var defaults = new Dictionary<string, string>
        {
            ["ShopName"]           = "TechStock Computer Shop",
            ["ShopAddress"]        = "Colombo, Sri Lanka",
            ["ShopPhone"]          = "+94 XX XXX XXXX",
            ["ShopEmail"]          = "info@techstock.lk",
            ["LowStockThreshold"]  = "5",
            ["InvoicePrefix"]      = "INV",
            ["BatchPrefix"]        = "BT",
            ["DefaultCurrency"]    = "JPY",
            ["InvoiceFooterNote"]  = "Thank you for shopping with us!",
            ["WarrantyEmail"]      = "warranty@techstock.lk",
        };

        foreach (var (key, value) in defaults)
        {
            if (!await db.AppSettings.AnyAsync(s => s.Key == key))
                db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
    }
}
