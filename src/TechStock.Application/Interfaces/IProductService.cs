using TechStock.Application.DTOs.Products;

namespace TechStock.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(ProductQueryParams query, bool includeCost);
    Task<ProductDto?> GetByIdAsync(Guid id, bool includeStock, bool includeCost);
    Task<int> GetTotalStockAsync(Guid productId);
    Task<ProductDto> CreateAsync(CreateProductRequest request);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request);
    Task DeleteAsync(Guid id);
}

public interface IProductTypeService
{
    Task<List<ProductTypeDto>> GetAllAsync();
    Task<ProductTypeDto?> GetByIdAsync(Guid id);
    Task<List<ConfigTypeDto>> GetConfigTypesAsync(Guid productTypeId);
    Task<ProductTypeDto> CreateAsync(CreateProductTypeRequest request);
    Task<ProductTypeDto> UpdateAsync(Guid id, UpdateProductTypeRequest request);
    Task DeleteAsync(Guid id);
}

public interface IConfigTypeService
{
    Task<List<ConfigTypeDto>> GetByProductTypeAsync(Guid productTypeId);
    Task<ConfigTypeDto> CreateAsync(CreateConfigTypeRequest request);
    Task<ConfigTypeDto> UpdateAsync(Guid id, UpdateConfigTypeRequest request);
    Task DeleteAsync(Guid id);
}
