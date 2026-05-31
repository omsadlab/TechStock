using AutoMapper;
using ClosedXML.Excel;
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
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(s => s.InvoiceNumber.Contains(query.Search) ||
                             (s.CustomerName != null && s.CustomerName.Contains(query.Search)));

        var total = await q.CountAsync();
        var ordered = (query.SortBy?.ToLower(), query.SortDir?.ToLower() == "desc") switch
        {
            ("invoice", false)  => q.OrderBy(s => s.InvoiceNumber),
            ("invoice", true)   => q.OrderByDescending(s => s.InvoiceNumber),
            ("customer", false) => q.OrderBy(s => s.CustomerName),
            ("customer", true)  => q.OrderByDescending(s => s.CustomerName),
            ("total", false)    => q.OrderBy(s => s.TotalLKR),
            ("total", true)     => q.OrderByDescending(s => s.TotalLKR),
            ("date", false)     => q.OrderBy(s => s.SaleDate),
            _                   => q.OrderByDescending(s => s.SaleDate),
        };
        var sales = await ordered
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

                var unitPrice = item.UnitSellingPrice ?? batchItem.SellingPriceLKR;
                var lineTotal = (unitPrice * item.Quantity) - item.Discount;
                subtotal += lineTotal;

                sale.Items.Add(new SaleItem
                {
                    BatchItemId = item.BatchItemId,
                    Quantity = item.Quantity,
                    UnitSellingPrice = unitPrice,
                    Discount = item.Discount,
                    LineTotal = lineTotal,
                    WarrantyMonths = item.WarrantyMonths,
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

    public async Task UpdateSaleAsync(Guid id, UpdateSaleRequest request)
    {
        var sale = await _db.Sales.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException($"Sale {id} not found.");
        sale.CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim();
        sale.CustomerPhone = string.IsNullOrWhiteSpace(request.CustomerPhone) ? null : request.CustomerPhone.Trim();
        sale.DiscountLKR = request.DiscountLKR;
        sale.TotalLKR = sale.SubtotalLKR - request.DiscountLKR;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateSaleItemAsync(Guid saleId, Guid itemId, UpdateSaleItemRequest request)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var sale = await _db.Sales.Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == saleId)
                ?? throw new NotFoundException($"Sale {saleId} not found.");
            var item = sale.Items.FirstOrDefault(i => i.Id == itemId)
                ?? throw new NotFoundException($"SaleItem {itemId} not found.");

            item.UnitSellingPrice = request.UnitSellingPrice;
            item.Discount = request.Discount;
            item.LineTotal = Math.Max(0, (request.UnitSellingPrice * item.Quantity) - request.Discount);
            item.WarrantyMonths = request.WarrantyMonths;

            sale.SubtotalLKR = sale.Items.Sum(i => i.LineTotal);
            sale.TotalLKR = sale.SubtotalLKR - sale.DiscountLKR;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task RemoveSaleItemAsync(Guid saleId, Guid itemId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var sale = await _db.Sales.Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == saleId)
                ?? throw new NotFoundException($"Sale {saleId} not found.");
            var item = sale.Items.FirstOrDefault(i => i.Id == itemId)
                ?? throw new NotFoundException($"SaleItem {itemId} not found.");

            var batchItem = await _db.BatchItems.FirstOrDefaultAsync(bi => bi.Id == item.BatchItemId)
                ?? throw new NotFoundException("BatchItem not found.");
            batchItem.RemainingQty += item.Quantity;

            var removedLineTotal = item.LineTotal;
            _db.SaleItems.Remove(item);
            sale.SubtotalLKR -= removedLineTotal;
            sale.TotalLKR = sale.SubtotalLKR - sale.DiscountLKR;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<SaleDto> AddSaleItemAsync(Guid saleId, CreateSaleItemRequest request)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var sale = await _db.Sales.Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == saleId)
                ?? throw new NotFoundException($"Sale {saleId} not found.");

            var batchItem = await _db.BatchItems.Include(bi => bi.Product)
                .FirstOrDefaultAsync(bi => bi.Id == request.BatchItemId)
                ?? throw new NotFoundException($"BatchItem {request.BatchItemId} not found.");

            if (batchItem.RemainingQty < request.Quantity)
                throw new BusinessException(
                    $"Insufficient stock for {batchItem.Product.Name}. Available: {batchItem.RemainingQty}");

            batchItem.RemainingQty -= request.Quantity;

            var unitPrice = request.UnitSellingPrice ?? batchItem.SellingPriceLKR;
            var lineTotal = Math.Max(0, (unitPrice * request.Quantity) - request.Discount);

            var newItem = new SaleItem
            {
                SaleId = saleId,
                BatchItemId = request.BatchItemId,
                Quantity = request.Quantity,
                UnitSellingPrice = unitPrice,
                Discount = request.Discount,
                LineTotal = lineTotal,
                WarrantyMonths = request.WarrantyMonths,
            };
            _db.SaleItems.Add(newItem);

            sale.SubtotalLKR += lineTotal;
            sale.TotalLKR = sale.SubtotalLKR - sale.DiscountLKR;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (await GetByIdAsync(saleId))!;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task<byte[]> ExportSaleExcelAsync(Guid id)
    {
        var sale = await GetByIdAsync(id) ?? throw new NotFoundException($"Sale {id} not found.");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Invoice");

        ws.Cell(1, 1).Value = $"Invoice: {sale.InvoiceNumber}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 4).Value = $"Date: {sale.SaleDate:dd MMM yyyy}";
        ws.Cell(2, 1).Value = $"Customer: {sale.CustomerName ?? "Walk-in"}";
        if (!string.IsNullOrEmpty(sale.CustomerPhone))
            ws.Cell(2, 4).Value = $"Phone: {sale.CustomerPhone}";

        var headers = new[] { "#", "Product", "Brand", "Barcode", "Warranty", "Qty", "Unit Price (LKR)", "Discount (LKR)", "Total (LKR)" };
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(4, c + 1).Value = headers[c];
            ws.Cell(4, c + 1).Style.Font.Bold = true;
            ws.Cell(4, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 5;
        for (int i = 0; i < sale.Items.Count; i++)
        {
            var item = sale.Items[i];
            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = item.ProductName;
            ws.Cell(row, 3).Value = item.BrandName;
            ws.Cell(row, 4).Value = item.Barcode ?? "";
            ws.Cell(row, 5).Value = item.WarrantyMonths.HasValue ? $"{item.WarrantyMonths} mo." : "—";
            ws.Cell(row, 6).Value = item.Quantity;
            ws.Cell(row, 7).Value = item.UnitSellingPrice;
            ws.Cell(row, 8).Value = item.Discount;
            ws.Cell(row, 9).Value = item.LineTotal;
            row++;
        }

        row++;
        ws.Cell(row, 8).Value = "Subtotal:";
        ws.Cell(row, 9).Value = sale.SubtotalLKR;
        row++;
        ws.Cell(row, 8).Value = "Discount:";
        ws.Cell(row, 9).Value = sale.DiscountLKR;
        row++;
        ws.Cell(row, 8).Value = "TOTAL";
        ws.Cell(row, 8).Style.Font.Bold = true;
        ws.Cell(row, 9).Value = sale.TotalLKR;
        ws.Cell(row, 9).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
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
