namespace TechStock.Domain.Entities;

public class Batch : BaseEntity
{
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string Currency { get; set; } = "JPY";
    public decimal ExchangeRate { get; set; }
    public decimal TotalCostLKR { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    public ICollection<BatchItem> Items { get; set; } = [];
}
