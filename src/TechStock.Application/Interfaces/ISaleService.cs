using TechStock.Application.DTOs.Products;
using TechStock.Application.DTOs.Sales;

namespace TechStock.Application.Interfaces;

public interface ISaleService
{
    Task<PagedResult<SaleDto>> GetSalesAsync(SaleQueryParams query, Guid? userId = null);
    Task<SaleDto?> GetByIdAsync(Guid id);
    Task<SaleDto> CreateAsync(CreateSaleRequest request, Guid userId);
}

public interface IStockAdjustmentService
{
    Task<List<StockAdjustmentDto>> GetByBatchItemAsync(Guid batchItemId);
    Task<StockAdjustmentDto> CreateAsync(CreateStockAdjustmentRequest request, Guid userId);
}
