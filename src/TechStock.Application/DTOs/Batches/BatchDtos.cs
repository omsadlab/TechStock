namespace TechStock.Application.DTOs.Batches;

public class BatchDto
{
    public Guid Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string PurchaseCurrency { get; set; } = "JPY";
    public string SellingCurrency { get; set; } = "LKR";
    public decimal ExchangeRate { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<BatchItemDto> Items { get; set; } = [];
}

public class WarrantyOptionDto
{
    public int WarrantyMonths { get; set; }
    public decimal SellingPriceLKR { get; set; }
    public bool IsDefault { get; set; }
}

public class WarrantyOptionRequest
{
    public int WarrantyMonths { get; set; }
    public decimal SellingPriceLKR { get; set; }
    public bool IsDefault { get; set; }
}

public class BatchItemDto
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string ProductTypeName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCostPurchase { get; set; }
    public decimal UnitCostLocal { get; set; }
    public decimal SellingPrice { get; set; }
    public int RemainingQty { get; set; }
    public string? Barcode { get; set; }
    public List<WarrantyOptionDto> WarrantyOptions { get; set; } = [];
}

public record BatchItemScanDto(
    Guid BatchItemId,
    Guid ProductId,
    string ProductName,
    string BrandName,
    string BatchNumber,
    string PurchaseCurrency,
    string SellingCurrency,
    decimal ExchangeRate,
    int RemainingQty,
    decimal SellingPrice,
    string Barcode
)
{
    public List<WarrantyOptionDto> WarrantyOptions { get; init; } = [];
};

public class CreateBatchRequest
{
    public DateTime PurchaseDate { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string PurchaseCurrency { get; set; } = "JPY";
    public string SellingCurrency { get; set; } = "LKR";
    public decimal ExchangeRate { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public string? Notes { get; set; }
    public List<CreateBatchItemRequest> Items { get; set; } = [];
}

public class UpdateBatchRequest
{
    public DateTime PurchaseDate { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string PurchaseCurrency { get; set; } = "JPY";
    public string SellingCurrency { get; set; } = "LKR";
    public decimal ExchangeRate { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public string? Notes { get; set; }
}

public class CreateBatchItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCostPurchase { get; set; }
    public decimal SellingPrice { get; set; }
    public List<WarrantyOptionRequest> WarrantyOptions { get; set; } = [];
}

public class UpdateBatchItemRequest
{
    public int Quantity { get; set; }
    public decimal UnitCostPurchase { get; set; }
    public decimal SellingPrice { get; set; }
    public List<WarrantyOptionRequest> WarrantyOptions { get; set; } = [];
}

public record UpdateSellingPriceRequest(decimal SellingPrice);

public class BatchQueryParams
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "date";
    public string SortDir { get; set; } = "desc";
}
