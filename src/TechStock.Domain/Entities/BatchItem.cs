namespace TechStock.Domain.Entities;

public class BatchItem : BaseEntity
{
    public Guid BatchId { get; set; }
    public Batch Batch { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitCostJPY { get; set; }
    public decimal UnitCostLKR { get; set; }
    public decimal SellingPriceLKR { get; set; }
    public int RemainingQty { get; set; }

    public ICollection<SaleItem> SaleItems { get; set; } = [];
    public ICollection<StockAdjustment> Adjustments { get; set; } = [];
}
