using Microsoft.EntityFrameworkCore;
using TechStock.Application.DTOs.Reports;
using TechStock.Application.Interfaces;
using TechStock.Infrastructure.Data;

namespace TechStock.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var thresholdStr = (await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "LowStockThreshold"))?.Value ?? "5";
        var threshold = int.Parse(thresholdStr);

        var totalStock = await _db.BatchItems.SumAsync(bi => (int?)bi.RemainingQty) ?? 0;

        var today = DateTime.UtcNow.Date;
        var todaySales = await _db.Sales
            .Where(s => s.SaleDate.Date == today)
            .SumAsync(s => (decimal?)s.TotalLKR) ?? 0m;

        var allProducts = await _db.Products.Select(p => p.Id).ToListAsync();
        int lowStockCount = 0;
        var lowStockItems = new List<LowStockItemDto>();
        foreach (var pid in allProducts)
        {
            var stock = await _db.BatchItems
                .Where(bi => bi.ProductId == pid && bi.RemainingQty > 0)
                .SumAsync(bi => (int?)bi.RemainingQty) ?? 0;
            if (stock <= threshold)
            {
                lowStockCount++;
                var product = await _db.Products.Include(p => p.Brand).FirstOrDefaultAsync(p => p.Id == pid);
                if (product != null)
                    lowStockItems.Add(new LowStockItemDto(pid, product.Name, product.Brand.Name, stock));
            }
        }

        var recentSales = await _db.Sales
            .OrderByDescending(s => s.SaleDate)
            .Take(10)
            .Select(s => new RecentSaleDto(s.Id, s.InvoiceNumber, s.SaleDate, s.TotalLKR, s.CustomerName))
            .ToListAsync();

        return new DashboardDto
        {
            TotalStockItems = totalStock,
            TodaySalesLKR = todaySales,
            LowStockCount = lowStockCount,
            PendingBatchesCount = 0,
            RecentSales = recentSales,
            LowStockItems = lowStockItems.Take(10).ToList(),
        };
    }

    public async Task<ProfitReportDto> GetProfitReportAsync(DateTime from, DateTime to)
    {
        var saleItems = await _db.SaleItems
            .Include(i => i.Sale)
            .Include(i => i.BatchItem).ThenInclude(bi => bi.Product).ThenInclude(p => p.Brand)
            .Include(i => i.BatchItem).ThenInclude(bi => bi.Product).ThenInclude(p => p.ProductType)
            .Where(i => i.Sale.SaleDate >= from && i.Sale.SaleDate <= to)
            .ToListAsync();

        var lines = saleItems
            .GroupBy(i => i.BatchItem.ProductId)
            .Select(g =>
            {
                var first = g.First();
                var revenue = g.Sum(i => i.LineTotal);
                var cost = g.Sum(i => i.BatchItem.UnitCostLKR * i.Quantity);
                var profit = revenue - cost;
                return new ProfitLineDto
                {
                    ProductName = first.BatchItem.Product.Name,
                    BrandName = first.BatchItem.Product.Brand.Name,
                    ProductTypeName = first.BatchItem.Product.ProductType.Name,
                    QtySold = g.Sum(i => i.Quantity),
                    RevenueLKR = revenue,
                    CostLKR = cost,
                    ProfitLKR = profit,
                    MarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100, 2) : 0,
                };
            })
            .OrderByDescending(l => l.ProfitLKR)
            .ToList();

        var totalRevenue = lines.Sum(l => l.RevenueLKR);
        var totalCost = lines.Sum(l => l.CostLKR);
        var grossProfit = totalRevenue - totalCost;

        return new ProfitReportDto
        {
            From = from,
            To = to,
            TotalRevenueLKR = totalRevenue,
            TotalCostLKR = totalCost,
            GrossProfitLKR = grossProfit,
            MarginPercent = totalRevenue > 0 ? Math.Round(grossProfit / totalRevenue * 100, 2) : 0,
            Lines = lines,
        };
    }

    public async Task<StockOnHandDto> GetStockOnHandAsync()
    {
        var items = await _db.BatchItems
            .Include(bi => bi.Product).ThenInclude(p => p.Brand)
            .Include(bi => bi.Product).ThenInclude(p => p.ProductType)
            .Include(bi => bi.Batch)
            .Where(bi => bi.RemainingQty > 0)
            .ToListAsync();

        var lines = items.Select(bi => new StockOnHandLineDto
        {
            ProductId = bi.ProductId,
            ProductName = bi.Product.Name,
            BrandName = bi.Product.Brand.Name,
            ProductTypeName = bi.Product.ProductType.Name,
            BatchNumber = bi.Batch.BatchNumber,
            RemainingQty = bi.RemainingQty,
            SellingPriceLKR = bi.SellingPriceLKR,
            StockValueLKR = bi.SellingPriceLKR * bi.RemainingQty,
        }).ToList();

        return new StockOnHandDto
        {
            Lines = lines,
            TotalStockValueLKR = lines.Sum(l => l.StockValueLKR),
        };
    }

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime from, DateTime to)
    {
        var sales = await _db.Sales
            .Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .ToListAsync();

        var lines = sales
            .GroupBy(s => s.SaleDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new SalesSummaryLineDto(g.Key, g.Count(), g.Sum(s => s.TotalLKR)))
            .ToList();

        return new SalesSummaryDto { Lines = lines, TotalLKR = lines.Sum(l => l.TotalLKR) };
    }

    public async Task<List<LowStockItemDto>> GetLowStockAsync(int threshold)
    {
        var result = new List<LowStockItemDto>();
        var products = await _db.Products.Include(p => p.Brand).ToListAsync();
        foreach (var p in products)
        {
            var stock = await _db.BatchItems
                .Where(bi => bi.ProductId == p.Id && bi.RemainingQty > 0)
                .SumAsync(bi => (int?)bi.RemainingQty) ?? 0;
            if (stock <= threshold)
                result.Add(new LowStockItemDto(p.Id, p.Name, p.Brand.Name, stock));
        }
        return result.OrderBy(r => r.TotalStock).ToList();
    }

    public async Task<ProfitReportDto> GetBatchProfitAsync(Guid batchId)
    {
        var saleItems = await _db.SaleItems
            .Include(i => i.Sale)
            .Include(i => i.BatchItem).ThenInclude(bi => bi.Product).ThenInclude(p => p.Brand)
            .Include(i => i.BatchItem).ThenInclude(bi => bi.Product).ThenInclude(p => p.ProductType)
            .Where(i => i.BatchItem.BatchId == batchId)
            .ToListAsync();

        var lines = saleItems
            .GroupBy(i => i.BatchItem.ProductId)
            .Select(g =>
            {
                var first = g.First();
                var revenue = g.Sum(i => i.LineTotal);
                var cost = g.Sum(i => i.BatchItem.UnitCostLKR * i.Quantity);
                var profit = revenue - cost;
                return new ProfitLineDto
                {
                    ProductName = first.BatchItem.Product.Name,
                    BrandName = first.BatchItem.Product.Brand.Name,
                    ProductTypeName = first.BatchItem.Product.ProductType.Name,
                    QtySold = g.Sum(i => i.Quantity),
                    RevenueLKR = revenue,
                    CostLKR = cost,
                    ProfitLKR = profit,
                    MarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100, 2) : 0,
                };
            })
            .OrderByDescending(l => l.ProfitLKR)
            .ToList();

        var totalRevenue = lines.Sum(l => l.RevenueLKR);
        var totalCost = lines.Sum(l => l.CostLKR);
        var grossProfit = totalRevenue - totalCost;

        return new ProfitReportDto
        {
            TotalRevenueLKR = totalRevenue,
            TotalCostLKR = totalCost,
            GrossProfitLKR = grossProfit,
            MarginPercent = totalRevenue > 0 ? Math.Round(grossProfit / totalRevenue * 100, 2) : 0,
            Lines = lines,
        };
    }
}
