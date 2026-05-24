namespace TechStock.Domain.Entities;

public class ProductType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = [];
    public ICollection<ConfigType> ConfigTypes { get; set; } = [];
}
