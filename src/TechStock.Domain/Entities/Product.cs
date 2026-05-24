namespace TechStock.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public Guid BrandId { get; set; }
    public Brand Brand { get; set; } = null!;
    public Guid ProductTypeId { get; set; }
    public ProductType ProductType { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProductConfig> Configs { get; set; } = [];
    public ICollection<BatchItem> BatchItems { get; set; } = [];
}
