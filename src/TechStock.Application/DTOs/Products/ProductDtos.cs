namespace TechStock.Application.DTOs.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public Guid BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public Guid ProductTypeId { get; set; }
    public string ProductTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int TotalStock { get; set; }
    public decimal? SellingPriceLKR { get; set; }
    public decimal? UnitCostLKR { get; set; }
    public decimal? UnitCostJPY { get; set; }
    public List<ProductConfigDto> Configs { get; set; } = [];
}

public record ProductConfigDto(Guid ConfigTypeId, string ConfigTypeName, string Value);

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public Guid BrandId { get; set; }
    public Guid ProductTypeId { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<ConfigValueRequest> Configs { get; set; } = [];
}

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public Guid BrandId { get; set; }
    public Guid ProductTypeId { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<ConfigValueRequest> Configs { get; set; } = [];
}

public record ConfigValueRequest(Guid ConfigTypeId, string Value);

public record ProductTypeDto(Guid Id, string Name, bool IsActive);

public record ConfigTypeDto(Guid Id, string Name, Guid ProductTypeId, int DisplayOrder, bool IsRequired);

public record CreateProductTypeRequest(string Name);

public record UpdateProductTypeRequest(string Name, bool IsActive);

public record CreateConfigTypeRequest(Guid ProductTypeId, string Name, int DisplayOrder, bool IsRequired);

public record UpdateConfigTypeRequest(string Name, int DisplayOrder, bool IsRequired);

public class ProductQueryParams
{
    public Guid? TypeId { get; set; }
    public Guid? BrandId { get; set; }
    public string? Search { get; set; }
    public bool? LowStock { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
