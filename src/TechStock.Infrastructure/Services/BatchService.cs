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
        var batches = await q.OrderByDescending(b => b.PurchaseDate)
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
            Currency = request.Currency,
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

        foreach (var item in items)
            AddItem(batch, item);

        batch.TotalCostLKR = batch.Items.Sum(i => i.UnitCostLKR * i.Quantity);
        await _db.SaveChangesAsync();
        return (await GetByIdAsync(batchId))!;
    }

    public async Task<BatchDto> UpdateItemAsync(Guid batchId, Guid itemId, UpdateBatchItemRequest request)
    {
        var batch = await _db.Batches.Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == batchId)
            ?? throw new NotFoundException($"Batch {batchId} not found.");

        var item = batch.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException($"BatchItem {itemId} not found.");

        item.Quantity = request.Quantity;
        item.UnitCostJPY = request.UnitCostJPY;
        item.UnitCostLKR = Math.Round(request.UnitCostJPY * batch.ExchangeRate, 2);
        item.SellingPriceLKR = request.SellingPriceLKR;
        item.UpdatedAt = DateTime.UtcNow;

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

    private static void AddItem(Batch batch, CreateBatchItemRequest request)
    {
        var unitCostLKR = Math.Round(request.UnitCostJPY * batch.ExchangeRate, 2);
        batch.Items.Add(new BatchItem
        {
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            UnitCostJPY = request.UnitCostJPY,
            UnitCostLKR = unitCostLKR,
            SellingPriceLKR = request.SellingPriceLKR,
            RemainingQty = request.Quantity,
        });
    }
}
