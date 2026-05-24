using TechStock.Domain.Enums;

namespace TechStock.Domain.Entities;

public class StockAdjustment : BaseEntity
{
    public Guid BatchItemId { get; set; }
    public BatchItem BatchItem { get; set; } = null!;
    public AdjustmentType Type { get; set; }
    public int QuantityChange { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid AdjustedBy { get; set; }
}
