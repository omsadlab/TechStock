namespace TechStock.Application.DTOs.Reports;

public class DashboardDto
{
    public int TotalStockItems { get; set; }
    public decimal TodaySalesLKR { get; set; }
    public int LowStockCount { get; set; }
    public int PendingBatchesCount { get; set; }
    public List<RecentSaleDto> RecentSales { get; set; } = [];
    public List<LowStockItemDto> LowStockItems { get; set; } = [];
}

public record RecentSaleDto(Guid Id, string InvoiceNumber, DateTime SaleDate, decimal TotalLKR, string? CustomerName);

public record LowStockItemDto(Guid ProductId, string ProductName, string BrandName, int TotalStock);

public class ProfitReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal TotalRevenueLKR { get; set; }
    public decimal TotalCostLKR { get; set; }
    public decimal GrossProfitLKR { get; set; }
    public decimal MarginPercent { get; set; }
    public List<ProfitLineDto> Lines { get; set; } = [];
}

public class ProfitLineDto
{
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ProductTypeName { get; set; } = string.Empty;
    public int QtySold { get; set; }
    public decimal RevenueLKR { get; set; }
    public decimal CostLKR { get; set; }
    public decimal ProfitLKR { get; set; }
    public decimal MarginPercent { get; set; }
}

public class StockOnHandDto
{
    public List<StockOnHandLineDto> Lines { get; set; } = [];
    public decimal TotalStockValueLKR { get; set; }
}

public class StockOnHandLineDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ProductTypeName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public int RemainingQty { get; set; }
    public decimal SellingPriceLKR { get; set; }
    public decimal StockValueLKR { get; set; }
}

public class SalesSummaryDto
{
    public List<SalesSummaryLineDto> Lines { get; set; } = [];
    public decimal TotalLKR { get; set; }
}

public record SalesSummaryLineDto(DateTime Date, int SaleCount, decimal TotalLKR);

public class ShopSettingsDto
{
    public string ShopName { get; set; } = string.Empty;
    public string ShopAddress { get; set; } = string.Empty;
    public string ShopPhone { get; set; } = string.Empty;
    public string ShopEmail { get; set; } = string.Empty;
    public string InvoiceFooterNote { get; set; } = string.Empty;
    public string WarrantyEmail { get; set; } = string.Empty;
}
