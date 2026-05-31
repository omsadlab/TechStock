using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TechStock.Application.DTOs.Batches;
using TechStock.Application.DTOs.Products;
using TechStock.Application.Exceptions;
using TechStock.Application.Interfaces;
using TechStock.Domain.Entities;
using TechStock.Infrastructure.Data;

namespace TechStock.Infrastructure.Services;

public class BatchService : IBatchService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public BatchService(AppDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<PagedResult<BatchDto>> GetBatchesAsync(BatchQueryParams query)
    {
        var q = _db.Batches.Include(b => b.Items).ThenInclude(i => i.Product)
            .ThenInclude(p => p.Brand)
            .Include(b => b.Items).ThenInclude(i => i.Product).ThenInclude(p => p.ProductType)
            .AsQueryable();

        if (query.From.HasValue) q = q.Where(b => b.PurchaseDate >= query.From.Value);
        if (query.To.HasValue) q = q.Where(b => b.PurchaseDate <= query.To.Value);

        var total = await q.CountAsync();
        var ordered = (query.SortBy?.ToLower(), query.SortDir?.ToLower() == "desc") switch
        {
            ("batchnumber", false) => q.OrderBy(b => b.BatchNumber),
            ("batchnumber", true)  => q.OrderByDescending(b => b.BatchNumber),
            ("supplier", false)    => q.OrderBy(b => b.Supplier),
            ("supplier", true)     => q.OrderByDescending(b => b.Supplier),
            ("totalcost", false)   => q.OrderBy(b => b.TotalCostLKR),
            ("totalcost", true)    => q.OrderByDescending(b => b.TotalCostLKR),
            ("date", false)        => q.OrderBy(b => b.PurchaseDate),
            _                      => q.OrderByDescending(b => b.PurchaseDate),
        };
        var batches = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<BatchDto>
        {
            Items = _mapper.Map<List<BatchDto>>(batches),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<BatchDto?> GetByIdAsync(Guid id)
    {
        var batch = await _db.Batches
            .Include(b => b.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Brand)
            .Include(b => b.Items).ThenInclude(i => i.Product).ThenInclude(p => p.ProductType)
            .Include(b => b.Items).ThenInclude(i => i.WarrantyOptions)
            .FirstOrDefaultAsync(b => b.Id == id);
        return batch == null ? null : _mapper.Map<BatchDto>(batch);
    }

    public async Task<BatchDto> CreateAsync(CreateBatchRequest request, Guid userId)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Batches.CountAsync(b => b.PurchaseDate.Year == year);
        var batch = new Batch
        {
            BatchNumber = $"BT-{year}-{(count + 1):D3}",
            PurchaseDate = request.PurchaseDate,
            Supplier = request.Supplier,
            Currency = request.PurchaseCurrency,
            SellingCurrency = request.SellingCurrency,
            ExchangeRate = request.ExchangeRate,
            EstimatedArrival = request.EstimatedArrival,
            Notes = request.Notes,
            CreatedBy = userId,
        };

        foreach (var item in request.Items)
            AddItem(batch, item);

        batch.TotalCostLKR = batch.Items.Sum(i => i.UnitCostLKR * i.Quantity);

        _db.Batches.Add(batch);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(batch.Id))!;
    }

    public async Task<BatchDto> UpdateAsync(Guid id, UpdateBatchRequest request)
    {
        var batch = await _db.Batches.Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Batch {id} not found.");

        batch.PurchaseDate = request.PurchaseDate;
        batch.Supplier = request.Supplier;
        batch.Currency = request.PurchaseCurrency;
        batch.SellingCurrency = request.SellingCurrency;
        batch.ExchangeRate = request.ExchangeRate;
        batch.EstimatedArrival = request.EstimatedArrival;
        batch.Notes = request.Notes;
        batch.UpdatedAt = DateTime.UtcNow;

        foreach (var item in batch.Items)
            item.UnitCostLKR = Math.Round(item.UnitCostJPY * batch.ExchangeRate, 2);

        batch.TotalCostLKR = batch.Items.Sum(i => i.UnitCostLKR * i.Quantity);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var batch = await _db.Batches.Include(b => b.Items).ThenInclude(i => i.SaleItems)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Batch {id} not found.");

        if (batch.Items.Any(i => i.SaleItems.Any()))
            throw new BusinessException("Cannot delete a batch that has associated sales.");

        _db.Batches.Remove(batch);
        await _db.SaveChangesAsync();
    }

    public async Task<BatchDto> AddItemsAsync(Guid batchId, List<CreateBatchItemRequest> items)
    {
        var batch = await _db.Batches.Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == batchId)
            ?? throw new NotFoundException($"Batch {batchId} not found.");

        var newItems = new List<BatchItem>();
        var existingCount = batch.Items.Count;
        for (int i = 0; i < items.Count; i++)
        {
            var request = items[i];
            var unitCostLocal = Math.Round(request.UnitCostPurchase * batch.ExchangeRate, 2);
            var newItem = new BatchItem
            {
                BatchId = batchId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UnitCostJPY = request.UnitCostPurchase,
                UnitCostLKR = unitCostLocal,
                SellingPriceLKR = request.SellingPrice,
                RemainingQty = request.Quantity,
                Barcode = GenerateBarcode(batch, existingCount + i + 1),
            };
            foreach (var wo in request.WarrantyOptions)
                newItem.WarrantyOptions.Add(new BatchItemWarrantyOption
                {
                    WarrantyMonths = wo.WarrantyMonths,
                    SellingPriceLKR = wo.SellingPriceLKR,
                    IsDefault = wo.IsDefault,
                });
            _db.BatchItems.Add(newItem);
            newItems.Add(newItem);
        }

        batch.TotalCostLKR = batch.Items.Sum(i => i.UnitCostLKR * i.Quantity)
            + newItems.Sum(i => i.UnitCostLKR * i.Quantity);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(batchId))!;
    }

    public async Task<BatchDto> UpdateItemAsync(Guid batchId, Guid itemId, UpdateBatchItemRequest request)
    {
        var batch = await _db.Batches.Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == batchId)
            ?? throw new NotFoundException($"Batch {batchId} not found.");

        var item = await _db.BatchItems.Include(i => i.WarrantyOptions)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.BatchId == batchId)
            ?? throw new NotFoundException($"BatchItem {itemId} not found.");

        item.Quantity = request.Quantity;
        item.UnitCostJPY = request.UnitCostPurchase;
        item.UnitCostLKR = Math.Round(request.UnitCostPurchase * batch.ExchangeRate, 2);
        item.SellingPriceLKR = request.SellingPrice;
        item.UpdatedAt = DateTime.UtcNow;

        _db.BatchItemWarrantyOptions.RemoveRange(item.WarrantyOptions);
        foreach (var wo in request.WarrantyOptions)
            item.WarrantyOptions.Add(new BatchItemWarrantyOption
            {
                WarrantyMonths = wo.WarrantyMonths,
                SellingPriceLKR = wo.SellingPriceLKR,
                IsDefault = wo.IsDefault,
            });

        batch.TotalCostLKR = batch.Items.Sum(i => i.UnitCostLKR * i.Quantity);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(batchId))!;
    }

    public async Task DeleteItemAsync(Guid batchId, Guid itemId)
    {
        var item = await _db.BatchItems.Include(i => i.SaleItems)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.BatchId == batchId)
            ?? throw new NotFoundException($"BatchItem {itemId} not found.");

        if (item.SaleItems.Any())
            throw new BusinessException("Cannot delete a batch item that has associated sales.");

        _db.BatchItems.Remove(item);

        var batch = await _db.Batches.Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == batchId);
        if (batch != null)
        {
            batch.TotalCostLKR = batch.Items
                .Where(i => i.Id != itemId)
                .Sum(i => i.UnitCostLKR * i.Quantity);
        }

        await _db.SaveChangesAsync();
    }

    public async Task UpdateSellingPriceAsync(Guid batchId, Guid itemId, decimal sellingPrice)
    {
        var item = await _db.BatchItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.BatchId == batchId)
            ?? throw new NotFoundException($"BatchItem {itemId} not found.");

        item.SellingPriceLKR = sellingPrice;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<BatchItemScanDto?> GetItemByBarcodeAsync(string barcode)
    {
        var item = await _db.BatchItems
            .Include(i => i.Product).ThenInclude(p => p.Brand)
            .Include(i => i.Batch)
            .Include(i => i.WarrantyOptions)
            .FirstOrDefaultAsync(i => i.Barcode == barcode);
        if (item == null) return null;
        return new BatchItemScanDto(
            item.Id, item.ProductId, item.Product.Name, item.Product.Brand.Name,
            item.Batch.BatchNumber, item.Batch.Currency, item.Batch.SellingCurrency,
            item.Batch.ExchangeRate, item.RemainingQty, item.SellingPriceLKR, item.Barcode!
        )
        {
            WarrantyOptions = item.WarrantyOptions.Select(ToWarrantyDto).ToList()
        };
    }

    public async Task<List<BatchItemScanDto>> SearchAvailableItemsAsync(string? search)
    {
        var q = _db.BatchItems
            .Include(i => i.Product).ThenInclude(p => p.Brand)
            .Include(i => i.Batch)
            .Include(i => i.WarrantyOptions)
            .Where(i => i.RemainingQty > 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(i =>
                i.Product.Name.ToLower().Contains(term) ||
                i.Product.Brand.Name.ToLower().Contains(term) ||
                (i.Barcode != null && i.Barcode.ToLower().Contains(term)));
        }

        var items = await q
            .OrderBy(i => i.Product.Name)
            .ThenBy(i => i.Batch.BatchNumber)
            .Take(100)
            .ToListAsync();

        return items.Select(i => new BatchItemScanDto(
            i.Id, i.ProductId, i.Product.Name, i.Product.Brand.Name,
            i.Batch.BatchNumber, i.Batch.Currency, i.Batch.SellingCurrency,
            i.Batch.ExchangeRate, i.RemainingQty, i.SellingPriceLKR, i.Barcode!
        )
        {
            WarrantyOptions = i.WarrantyOptions.Select(ToWarrantyDto).ToList()
        }).ToList();
    }

    private static WarrantyOptionDto ToWarrantyDto(BatchItemWarrantyOption wo) =>
        new() { WarrantyMonths = wo.WarrantyMonths, SellingPriceLKR = wo.SellingPriceLKR, IsDefault = wo.IsDefault };

    private static void AddItem(Batch batch, CreateBatchItemRequest request)
    {
        var itemSeq = batch.Items.Count + 1;
        var unitCostLocal = Math.Round(request.UnitCostPurchase * batch.ExchangeRate, 2);
        var item = new BatchItem
        {
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            UnitCostJPY = request.UnitCostPurchase,
            UnitCostLKR = unitCostLocal,
            SellingPriceLKR = request.SellingPrice,
            RemainingQty = request.Quantity,
            Barcode = GenerateBarcode(batch, itemSeq),
        };
        foreach (var wo in request.WarrantyOptions)
            item.WarrantyOptions.Add(new BatchItemWarrantyOption
            {
                WarrantyMonths = wo.WarrantyMonths,
                SellingPriceLKR = wo.SellingPriceLKR,
                IsDefault = wo.IsDefault,
            });
        batch.Items.Add(item);
    }

    private static string GenerateBarcode(Batch batch, int itemSeq)
    {
        var parts = batch.BatchNumber.Split('-');
        var year = parts.Length > 1 ? parts[1] : DateTime.UtcNow.Year.ToString();
        var batchSeq = parts.Length > 2 ? parts[2] : "001";
        return $"TS-{year}-{batchSeq}-{itemSeq:D2}";
    }
}
