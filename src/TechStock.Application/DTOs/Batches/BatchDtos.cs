namespace TechStock.Application.DTOs.Batches;

public class BatchDto
{
    public Guid Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string Currency { get; set; } = "JPY";
    public decimal ExchangeRate { get; set; }
    public decimal TotalCostLKR { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<BatchItemDto> Items { get; set; } = [];
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
    public decimal UnitCostJPY { get; set; }
    public decimal UnitCostLKR { get; set; }
    public decimal SellingPriceLKR { get; set; }
    public int RemainingQty { get; set; }
}

public class CreateBatchRequest
{
    public DateTime PurchaseDate { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string Currency { get; set; } = "JPY";
    public decimal ExchangeRate { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public string? Notes { get; set; }
    public List<CreateBatchItemRequest> Items { get; set; } = [];
}

public class UpdateBatchRequest
{
    public DateTime PurchaseDate { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public string? Notes { get; set; }
}

public class CreateBatchItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCostJPY { get; set; }
    public decimal SellingPriceLKR { get; set; }
}

public class UpdateBatchItemRequest
{
    public int Quantity { get; set; }
    public decimal UnitCostJPY { get; set; }
    public decimal SellingPriceLKR { get; set; }
}

public record UpdateSellingPriceRequest(decimal SellingPriceLKR);

public class BatchQueryParams
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
