using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechStock.Domain.Entities;

namespace TechStock.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<ConfigType> ConfigTypes => Set<ConfigType>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductConfig> ProductConfigs => Set<ProductConfig>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchItem> BatchItems => Set<BatchItem>();
    public DbSet<BatchItemWarrantyOption> BatchItemWarrantyOptions => Set<BatchItemWarrantyOption>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<WarrantyClaim> WarrantyClaims => Set<WarrantyClaim>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Batch>(e =>
        {
            e.Property(x => x.ExchangeRate).HasPrecision(10, 6);
            e.Property(x => x.TotalCostLKR).HasPrecision(18, 2);
        });

        builder.Entity<BatchItem>(e =>
        {
            e.Property(x => x.UnitCostJPY).HasPrecision(18, 4);
            e.Property(x => x.UnitCostLKR).HasPrecision(18, 4);
            e.Property(x => x.SellingPriceLKR).HasPrecision(18, 2);
        });

        builder.Entity<Sale>(e =>
        {
            e.Property(x => x.SubtotalLKR).HasPrecision(18, 2);
            e.Property(x => x.DiscountLKR).HasPrecision(18, 2);
            e.Property(x => x.TotalLKR).HasPrecision(18, 2);
        });

        builder.Entity<SaleItem>(e =>
        {
            e.Property(x => x.UnitSellingPrice).HasPrecision(18, 2);
            e.Property(x => x.Discount).HasPrecision(18, 2);
            e.Property(x => x.LineTotal).HasPrecision(18, 2);
        });

        builder.Entity<Brand>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<ProductType>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<Batch>().HasIndex(x => x.BatchNumber).IsUnique();
        builder.Entity<Sale>().HasIndex(x => x.InvoiceNumber).IsUnique();

        builder.Entity<Product>().HasQueryFilter(x => x.IsActive);
        builder.Entity<Brand>().HasQueryFilter(x => x.IsActive);
        builder.Entity<ProductType>().HasQueryFilter(x => x.IsActive);

        // SQL Server disallows multiple cascade paths — use Restrict on all FKs
        // and handle deletions in application code instead.
        builder.Entity<ProductConfig>()
            .HasOne(x => x.Product).WithMany(x => x.Configs)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ProductConfig>()
            .HasOne(x => x.ConfigType).WithMany(x => x.ProductConfigs)
            .HasForeignKey(x => x.ConfigTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BatchItem>()
            .HasOne(x => x.Product).WithMany(x => x.BatchItems)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<BatchItem>()
            .HasOne(x => x.Batch).WithMany(x => x.Items)
            .HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SaleItem>()
            .HasOne(x => x.Sale).WithMany(x => x.Items)
            .HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SaleItem>()
            .HasOne(x => x.BatchItem).WithMany(x => x.SaleItems)
            .HasForeignKey(x => x.BatchItemId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockAdjustment>()
            .HasOne(x => x.BatchItem).WithMany(x => x.Adjustments)
            .HasForeignKey(x => x.BatchItemId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BatchItemWarrantyOption>(e =>
        {
            e.Property(x => x.SellingPriceLKR).HasPrecision(18, 2);
            e.HasOne(x => x.BatchItem).WithMany(x => x.WarrantyOptions)
             .HasForeignKey(x => x.BatchItemId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ConfigType>()
            .HasOne(x => x.ProductType).WithMany(x => x.ConfigTypes)
            .HasForeignKey(x => x.ProductTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WarrantyClaim>(e =>
        {
            e.HasIndex(x => x.ClaimNumber).IsUnique();
            e.Property(x => x.ReplacementCostLKR).HasPrecision(18, 4);
            e.HasOne(x => x.SaleItem).WithMany()
             .HasForeignKey(x => x.SaleItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReplacementBatchItem).WithMany()
             .HasForeignKey(x => x.ReplacementBatchItemId).OnDelete(DeleteBehavior.Restrict)
             .IsRequired(false);
        });
    }
}
