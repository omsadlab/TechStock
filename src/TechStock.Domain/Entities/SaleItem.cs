namespace TechStock.Domain.Entities;

public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public Guid BatchItemId { get; set; }
    public BatchItem BatchItem { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitSellingPrice { get; set; }
    public decimal Discount { get; set; } = 0;
    public decimal LineTotal { get; set; }
    public int? WarrantyMonths { get; set; }
}
