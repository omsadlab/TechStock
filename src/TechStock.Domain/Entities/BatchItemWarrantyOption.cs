namespace TechStock.Domain.Entities;

public class BatchItemWarrantyOption : BaseEntity
{
    public Guid BatchItemId { get; set; }
    public BatchItem BatchItem { get; set; } = null!;
    public int WarrantyMonths { get; set; }
    public decimal SellingPriceLKR { get; set; }
    public bool IsDefault { get; set; }
}
