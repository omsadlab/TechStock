namespace TechStock.Application.DTOs.Sales;

public class SaleDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public decimal SubtotalLKR { get; set; }
    public decimal DiscountLKR { get; set; }
    public decimal TotalLKR { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public List<SaleItemDto> Items { get; set; } = [];
}

public class SaleItemDto
{
    public Guid Id { get; set; }
    public Guid BatchItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int Quantity { get; set; }
    public decimal UnitSellingPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
    public int? WarrantyMonths { get; set; }
}

public class CreateSaleRequest
{
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public decimal DiscountLKR { get; set; } = 0;
    public List<CreateSaleItemRequest> Items { get; set; } = [];
}

public class CreateSaleItemRequest
{
    public Guid BatchItemId { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; } = 0;
    public decimal? UnitSellingPrice { get; set; }
    public int? WarrantyMonths { get; set; }
}

public class SaleQueryParams
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "date";
    public string SortDir { get; set; } = "desc";
}

public class StockAdjustmentDto
{
    public Guid Id { get; set; }
    public Guid BatchItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid AdjustedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateSaleRequest
{
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public decimal DiscountLKR { get; set; }
}

public class UpdateSaleItemRequest
{
    public decimal UnitSellingPrice { get; set; }
    public decimal Discount { get; set; }
    public int? WarrantyMonths { get; set; }
}

public class CreateStockAdjustmentRequest
{
    public Guid BatchItemId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public string Reason { get; set; } = string.Empty;
}
