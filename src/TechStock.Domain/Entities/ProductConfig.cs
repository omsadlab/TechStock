namespace TechStock.Domain.Entities;

public class ProductConfig : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid ConfigTypeId { get; set; }
    public ConfigType ConfigType { get; set; } = null!;
    public string Value { get; set; } = string.Empty;
}
