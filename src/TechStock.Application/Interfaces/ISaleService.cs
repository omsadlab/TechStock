using TechStock.Application.DTOs.Products;
using TechStock.Application.DTOs.Sales;

namespace TechStock.Application.Interfaces;

public interface ISaleService
{
    Task<PagedResult<SaleDto>> GetSalesAsync(SaleQueryParams query, Guid? userId = null);
    Task<SaleDto?> GetByIdAsync(Guid id);
    Task<SaleDto> CreateAsync(CreateSaleRequest request, Guid userId);
    Task UpdateSaleAsync(Guid id, UpdateSaleRequest request);
    Task UpdateSaleItemAsync(Guid saleId, Guid itemId, UpdateSaleItemRequest request);
    Task RemoveSaleItemAsync(Guid saleId, Guid itemId);
    Task<SaleDto> AddSaleItemAsync(Guid saleId, CreateSaleItemRequest request);
    Task<byte[]> ExportSaleExcelAsync(Guid id);
}

public interface IStockAdjustmentService
{
    Task<List<StockAdjustmentDto>> GetByBatchItemAsync(Guid batchItemId);
    Task<StockAdjustmentDto> CreateAsync(CreateStockAdjustmentRequest request, Guid userId);
}
