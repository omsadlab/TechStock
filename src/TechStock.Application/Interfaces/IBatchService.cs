using TechStock.Application.DTOs.Batches;
using TechStock.Application.DTOs.Products;

namespace TechStock.Application.Interfaces;

public interface IBatchService
{
    Task<PagedResult<BatchDto>> GetBatchesAsync(BatchQueryParams query);
    Task<BatchDto?> GetByIdAsync(Guid id);
    Task<BatchDto> CreateAsync(CreateBatchRequest request, Guid userId);
    Task<BatchDto> UpdateAsync(Guid id, UpdateBatchRequest request);
    Task DeleteAsync(Guid id);
    Task<BatchDto> AddItemsAsync(Guid batchId, List<CreateBatchItemRequest> items);
    Task<BatchDto> UpdateItemAsync(Guid batchId, Guid itemId, UpdateBatchItemRequest request);
    Task DeleteItemAsync(Guid batchId, Guid itemId);
    Task UpdateSellingPriceAsync(Guid batchId, Guid itemId, decimal sellingPrice);
    Task<BatchItemScanDto?> GetItemByBarcodeAsync(string barcode);
    Task<List<BatchItemScanDto>> SearchAvailableItemsAsync(string? search);
}
