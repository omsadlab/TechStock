namespace TechStock.Domain.Entities;

public class ConfigType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid ProductTypeId { get; set; }
    public ProductType ProductType { get; set; } = null!;
    public int DisplayOrder { get; set; } = 0;
    public bool IsRequired { get; set; } = false;

    public ICollection<ProductConfig> ProductConfigs { get; set; } = [];
}
