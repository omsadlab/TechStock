namespace TechStock.Application.DTOs.Claims;

public class WarrantyClaimDto
{
    public Guid Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public DateTime ClaimedAt { get; set; }
    public Guid SaleId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public Guid SaleItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int? WarrantyMonths { get; set; }
    public string ClaimType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ComponentName { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Replacement item details
    public Guid? ReplacementBatchItemId { get; set; }
    public string? ReplacementProductName { get; set; }
    public string? ReplacementBrandName { get; set; }
    public string? ReplacementBarcode { get; set; }
    public string? ReplacementBatchNumber { get; set; }
    public decimal? ReplacementCostLKR { get; set; }
    public bool StockDeducted { get; set; }
}

public class ReplacementCandidateDto
{
    public Guid BatchItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int RemainingQty { get; set; }
    public decimal UnitCostLKR { get; set; }
    public decimal SellingPriceLKR { get; set; }
}

public class CreateWarrantyClaimRequest
{
    public Guid SaleItemId { get; set; }
    public string ClaimType { get; set; } = string.Empty;
    public string? ComponentName { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public DateTime? ClaimedAt { get; set; }
}

public class UpdateClaimStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? ResolutionNotes { get; set; }
    public Guid? ReplacementBatchItemId { get; set; }
}

public class ClaimQueryParams
{
    public string? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? BatchItemId { get; set; }
    public string? BatchNumber { get; set; }
    public string? Barcode { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
