using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TechStock.Application.DTOs.Products;
using TechStock.Application.DTOs.Sales;
using TechStock.Application.Exceptions;
using TechStock.Application.Interfaces;
using TechStock.Domain.Entities;
using TechStock.Domain.Enums;
using TechStock.Infrastructure.Data;

namespace TechStock.Infrastructure.Services;

public class SaleService : ISaleService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public SaleService(AppDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<PagedResult<SaleDto>> GetSalesAsync(SaleQueryParams query, Guid? userId = null)
    {
        var q = _db.Sales
            .Include(s => s.Items).ThenInclude(i => i.BatchItem).ThenInclude(bi => bi.Product).ThenInclude(p => p.Brand)
            .AsQueryable();

        if (userId.HasValue) q = q.Where(s => s.CreatedBy == userId.Value);
        if (query.From.HasValue) q = q.Where(s => s.SaleDate >= query.From.Value);
        if (query.To.HasValue) q = q.Where(s => s.SaleDate <= query.To.Value);

        var total = await q.CountAsync();
        var sales = await q.OrderByDescending(s => s.SaleDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<SaleDto>
        {
            Items = _mapper.Map<List<SaleDto>>(sales),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<SaleDto?> GetByIdAsync(Guid id)
    {
        var sale = await _db.Sales
            .Include(s => s.Items).ThenInclude(i => i.BatchItem).ThenInclude(bi => bi.Product).ThenInclude(p => p.Brand)
            .FirstOrDefaultAsync(s => s.Id == id);
        return sale == null ? null : _mapper.Map<SaleDto>(sale);
    }

    public async Task<SaleDto> CreateAsync(CreateSaleRequest request, Guid userId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var today = DateTime.UtcNow.Date;
            var count = await _db.Sales.CountAsync(s => s.SaleDate.Date == today);

            var sale = new Sale
            {
                InvoiceNumber = $"INV-{today:yyyyMMdd}-{(count + 1):D3}",
                SaleDate = DateTime.UtcNow,
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                DiscountLKR = request.DiscountLKR,
                CreatedBy = userId,
            };

            decimal subtotal = 0;
            foreach (var item in request.Items)
            {
                var batchItem = await _db.BatchItems
                    .Include(bi => bi.Product)
                    .FirstOrDefaultAsync(bi => bi.Id == item.BatchItemId)
                    ?? throw new NotFoundException($"BatchItem {item.BatchItemId} not found.");

                if (batchItem.RemainingQty < item.Quantity)
                    throw new BusinessException(
                        $"Insufficient stock for {batchItem.Product.Name}. Available: {batchItem.RemainingQty}");

                batchItem.RemainingQty -= item.Quantity;

                var lineTotal = (batchItem.SellingPriceLKR * item.Quantity) - item.Discount;
                subtotal += lineTotal;

                sale.Items.Add(new SaleItem
                {
                    BatchItemId = item.BatchItemId,
                    Quantity = item.Quantity,
                    UnitSellingPrice = batchItem.SellingPriceLKR,
                    Discount = item.Discount,
                    LineTotal = lineTotal,
                });
            }

            sale.SubtotalLKR = subtotal;
            sale.TotalLKR = subtotal - request.DiscountLKR;

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (await GetByIdAsync(sale.Id))!;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}

public class StockAdjustmentService : IStockAdjustmentService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public StockAdjustmentService(AppDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<List<StockAdjustmentDto>> GetByBatchItemAsync(Guid batchItemId) =>
        await _db.StockAdjustments
            .Include(a => a.BatchItem).ThenInclude(bi => bi.Product)
            .Where(a => a.BatchItemId == batchItemId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new StockAdjustmentDto
            {
                Id = a.Id,
                BatchItemId = a.BatchItemId,
                ProductName = a.BatchItem.Product.Name,
                Type = a.Type.ToString(),
                QuantityChange = a.QuantityChange,
                Reason = a.Reason,
                AdjustedBy = a.AdjustedBy,
                CreatedAt = a.CreatedAt,
            })
            .ToListAsync();

    public async Task<StockAdjustmentDto> CreateAsync(CreateStockAdjustmentRequest request, Guid userId)
    {
        if (!Enum.TryParse<AdjustmentType>(request.Type, true, out var type))
            throw new BusinessException($"Invalid adjustment type: {request.Type}");

        var batchItem = await _db.BatchItems.Include(bi => bi.Product)
            .FirstOrDefaultAsync(bi => bi.Id == request.BatchItemId)
            ?? throw new NotFoundException($"BatchItem {request.BatchItemId} not found.");

        batchItem.RemainingQty += request.QuantityChange;
        if (batchItem.RemainingQty < 0)
            throw new BusinessException("Stock cannot go below zero.");

        var adjustment = new StockAdjustment
        {
            BatchItemId = request.BatchItemId,
            Type = type,
            QuantityChange = request.QuantityChange,
            Reason = request.Reason,
            AdjustedBy = userId,
        };

        _db.StockAdjustments.Add(adjustment);
        await _db.SaveChangesAsync();

        return new StockAdjustmentDto
        {
            Id = adjustment.Id,
            BatchItemId = adjustment.BatchItemId,
            ProductName = batchItem.Product.Name,
            Type = adjustment.Type.ToString(),
            QuantityChange = adjustment.QuantityChange,
            Reason = adjustment.Reason,
            AdjustedBy = adjustment.AdjustedBy,
            CreatedAt = adjustment.CreatedAt,
        };
    }
}
