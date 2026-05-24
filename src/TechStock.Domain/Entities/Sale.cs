namespace TechStock.Domain.Entities;

public class Sale : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public decimal SubtotalLKR { get; set; }
    public decimal DiscountLKR { get; set; } = 0;
    public decimal TotalLKR { get; set; }
    public Guid CreatedBy { get; set; }

    public ICollection<SaleItem> Items { get; set; } = [];
}
