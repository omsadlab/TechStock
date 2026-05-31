using TechStock.Domain.Enums;

namespace TechStock.Domain.Entities;

public class WarrantyClaim : BaseEntity
{
    public string ClaimNumber { get; set; } = string.Empty;
    public Guid SaleItemId { get; set; }
    public SaleItem SaleItem { get; set; } = null!;
    public ClaimType ClaimType { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Pending;
    public string? ComponentName { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public DateTime ClaimedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? ResolvedBy { get; set; }

    // Replacement item (taken from current inventory when resolved)
    public Guid? ReplacementBatchItemId { get; set; }
    public BatchItem? ReplacementBatchItem { get; set; }
    public decimal? ReplacementCostLKR { get; set; }   // purchase cost of the replacement item
    public bool StockDeducted { get; set; }             // prevents double-deduction if status updated again
}
