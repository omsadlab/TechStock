using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TechStock.Application.DTOs.Products;
using TechStock.Application.Exceptions;
using TechStock.Application.Interfaces;
using TechStock.Domain.Entities;
using TechStock.Infrastructure.Data;

namespace TechStock.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public ProductService(AppDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(ProductQueryParams query, bool includeCost)
    {
        var q = _db.Products
            .Include(p => p.Brand)
            .Include(p => p.ProductType)
            .Include(p => p.Configs).ThenInclude(c => c.ConfigType)
            .AsQueryable();

        if (query.TypeId.HasValue) q = q.Where(p => p.ProductTypeId == query.TypeId.Value);
        if (query.BrandId.HasValue) q = q.Where(p => p.BrandId == query.BrandId.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(p => p.Name.Contains(query.Search) || (p.Model != null && p.Model.Contains(query.Search)));

        var totalCount = await q.CountAsync();
        var ordered = (query.SortBy?.ToLower(), query.SortDir?.ToLower() == "desc") switch
        {
            ("brand", false)  => q.OrderBy(p => p.Brand.Name).ThenBy(p => p.Name),
            ("brand", true)   => q.OrderByDescending(p => p.Brand.Name).ThenBy(p => p.Name),
            ("type", false)   => q.OrderBy(p => p.ProductType.Name).ThenBy(p => p.Name),
            ("type", true)    => q.OrderByDescending(p => p.ProductType.Name).ThenBy(p => p.Name),
            ("status", false) => q.OrderBy(p => p.IsActive).ThenBy(p => p.Name),
            ("status", true)  => q.OrderByDescending(p => p.IsActive).ThenBy(p => p.Name),
            (_, true)         => q.OrderByDescending(p => p.Name),
            _                 => q.OrderBy(p => p.Name),
        };
        var products = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var lowStockThreshold = 5;
        var dtos = new List<ProductDto>();
        foreach (var p in products)
        {
            var dto = _mapper.Map<ProductDto>(p);
            dto.Configs = p.Configs.Select(c => _mapper.Map<ProductConfigDto>(c)).ToList();

            var stock = await GetTotalStockAsync(p.Id);
            dto.TotalStock = stock;

            var latestItem = await _db.BatchItems
                .Where(bi => bi.ProductId == p.Id)
                .OrderByDescending(bi => bi.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestItem != null)
            {
                dto.SellingPriceLKR = latestItem.SellingPriceLKR;
                if (includeCost)
                {
                    dto.UnitCostLKR = latestItem.UnitCostLKR;
                    dto.UnitCostJPY = latestItem.UnitCostJPY;
                }
            }

            dtos.Add(dto);
        }

        if (query.LowStock == true)
            dtos = dtos.Where(d => d.TotalStock <= lowStockThreshold).ToList();

        return new PagedResult<ProductDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, bool includeStock, bool includeCost)
    {
        var product = await _db.Products
            .Include(p => p.Brand)
            .Include(p => p.ProductType)
            .Include(p => p.Configs).ThenInclude(c => c.ConfigType)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        var dto = _mapper.Map<ProductDto>(product);
        dto.Configs = product.Configs.Select(c => _mapper.Map<ProductConfigDto>(c)).ToList();

        if (includeStock) dto.TotalStock = await GetTotalStockAsync(id);

        var latestItem = await _db.BatchItems
            .Where(bi => bi.ProductId == id)
            .OrderByDescending(bi => bi.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestItem != null)
        {
            dto.SellingPriceLKR = latestItem.SellingPriceLKR;
            if (includeCost)
            {
                dto.UnitCostLKR = latestItem.UnitCostLKR;
                dto.UnitCostJPY = latestItem.UnitCostJPY;
            }
        }

        return dto;
    }

    public async Task<int> GetTotalStockAsync(Guid productId) =>
        await _db.BatchItems
            .Where(bi => bi.ProductId == productId && bi.RemainingQty > 0)
            .SumAsync(bi => (int?)bi.RemainingQty) ?? 0;

    public async Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Model = request.Model,
            BrandId = request.BrandId,
            ProductTypeId = request.ProductTypeId,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
        };

        foreach (var cfg in request.Configs)
            product.Configs.Add(new ProductConfig { ConfigTypeId = cfg.ConfigTypeId, Value = cfg.Value });

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(product.Id, false, true))!;
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _db.Products.Include(p => p.Configs)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Product {id} not found.");

        product.Name = request.Name;
        product.Model = request.Model;
        product.BrandId = request.BrandId;
        product.ProductTypeId = request.ProductTypeId;
        product.Description = request.Description;
        product.ImageUrl = request.ImageUrl;
        product.UpdatedAt = DateTime.UtcNow;

        _db.ProductConfigs.RemoveRange(product.Configs);
        foreach (var cfg in request.Configs)
            product.Configs.Add(new ProductConfig { ConfigTypeId = cfg.ConfigTypeId, Value = cfg.Value });

        await _db.SaveChangesAsync();
        return (await GetByIdAsync(product.Id, true, true))!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _db.Products.FindAsync(id)
            ?? throw new NotFoundException($"Product {id} not found.");
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<List<ProductBatchSummaryDto>> GetProductBatchesAsync(Guid productId, bool includeCost, DateTime? from, DateTime? to)
    {
        var q = _db.BatchItems
            .Include(bi => bi.Batch)
            .Include(bi => bi.SaleItems)
            .Where(bi => bi.ProductId == productId);

        if (from.HasValue) q = q.Where(bi => bi.Batch.PurchaseDate >= from.Value);
        if (to.HasValue) q = q.Where(bi => bi.Batch.PurchaseDate < to.Value.AddDays(1));

        var items = await q.OrderByDescending(bi => bi.Batch.PurchaseDate).ToListAsync();

        var batchItemIds = items.Select(bi => bi.Id).ToList();
        var claimCounts = await _db.WarrantyClaims
            .Where(c => c.ReplacementBatchItemId.HasValue &&
                        batchItemIds.Contains(c.ReplacementBatchItemId!.Value) &&
                        c.StockDeducted)
            .GroupBy(c => c.ReplacementBatchItemId!.Value)
            .Select(g => new { BatchItemId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BatchItemId, x => x.Count);

        return items.Select(bi =>
        {
            var claimed = claimCounts.GetValueOrDefault(bi.Id, 0);
            return new ProductBatchSummaryDto
            {
                BatchItemId = bi.Id,
                BatchId = bi.BatchId,
                BatchNumber = bi.Batch.BatchNumber,
                PurchaseDate = bi.Batch.PurchaseDate,
                UnitCostJPY = includeCost ? bi.UnitCostJPY : null,
                UnitCostLKR = includeCost ? bi.UnitCostLKR : null,
                SellingPriceLKR = bi.SellingPriceLKR,
                PurchasedQty = bi.Quantity,
                CostTotal = includeCost ? (decimal?)(bi.UnitCostLKR * bi.Quantity) : null,
                SoldQty = bi.Quantity - bi.RemainingQty - claimed,
                ClaimedQty = claimed,
                RemainingQty = bi.RemainingQty,
                SoldAmount = bi.SaleItems.Sum(si => si.LineTotal),
            };
        }).ToList();
    }

    public async Task<List<ProductSaleLineDto>> GetProductSalesAsync(Guid productId, DateTime? from, DateTime? to, Guid? createdByUserId = null)
    {
        var q = _db.SaleItems
            .Where(si => si.BatchItem.ProductId == productId);

        if (from.HasValue) q = q.Where(si => si.Sale.SaleDate >= from.Value);
        if (to.HasValue) q = q.Where(si => si.Sale.SaleDate < to.Value.AddDays(1));
        if (createdByUserId.HasValue) q = q.Where(si => si.Sale.CreatedBy == createdByUserId.Value);

        var raw = await q
            .Select(si => new
            {
                si.SaleId,
                si.Sale.InvoiceNumber,
                si.Sale.SaleDate,
                si.Quantity,
                si.LineTotal,
            })
            .ToListAsync();

        return raw
            .GroupBy(x => new { x.SaleId, x.InvoiceNumber, x.SaleDate })
            .Select(g => new ProductSaleLineDto
            {
                SaleId = g.Key.SaleId,
                InvoiceNumber = g.Key.InvoiceNumber,
                SaleDate = g.Key.SaleDate,
                Quantity = g.Sum(x => x.Quantity),
                LineTotal = g.Sum(x => x.LineTotal),
            })
            .OrderByDescending(x => x.SaleDate)
            .ToList();
    }
}

public class ProductTypeService : IProductTypeService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public ProductTypeService(AppDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<List<ProductTypeDto>> GetAllAsync() =>
        _mapper.Map<List<ProductTypeDto>>(await _db.ProductTypes.OrderBy(t => t.Name).ToListAsync());

    public async Task<ProductTypeDto?> GetByIdAsync(Guid id) =>
        _mapper.Map<ProductTypeDto>(await _db.ProductTypes.FindAsync(id));

    public async Task<List<ConfigTypeDto>> GetConfigTypesAsync(Guid productTypeId) =>
        _mapper.Map<List<ConfigTypeDto>>(
            await _db.ConfigTypes
                .Where(c => c.ProductTypeId == productTypeId)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync());

    public async Task<ProductTypeDto> CreateAsync(CreateProductTypeRequest request)
    {
        var pt = new ProductType { Name = request.Name };
        _db.ProductTypes.Add(pt);
        await _db.SaveChangesAsync();
        return _mapper.Map<ProductTypeDto>(pt);
    }

    public async Task<ProductTypeDto> UpdateAsync(Guid id, UpdateProductTypeRequest request)
    {
        var pt = await _db.ProductTypes.FindAsync(id)
            ?? throw new NotFoundException($"ProductType {id} not found.");
        pt.Name = request.Name;
        pt.IsActive = request.IsActive;
        pt.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return _mapper.Map<ProductTypeDto>(pt);
    }

    public async Task DeleteAsync(Guid id)
    {
        var pt = await _db.ProductTypes.FindAsync(id)
            ?? throw new NotFoundException($"ProductType {id} not found.");
        pt.IsActive = false;
        await _db.SaveChangesAsync();
    }
}

public class ConfigTypeService : IConfigTypeService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public ConfigTypeService(AppDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<List<ConfigTypeDto>> GetByProductTypeAsync(Guid productTypeId) =>
        _mapper.Map<List<ConfigTypeDto>>(
            await _db.ConfigTypes
                .Where(c => c.ProductTypeId == productTypeId)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync());

    public async Task<ConfigTypeDto> CreateAsync(CreateConfigTypeRequest request)
    {
        var ct = new ConfigType
        {
            ProductTypeId = request.ProductTypeId,
            Name = request.Name,
            DisplayOrder = request.DisplayOrder,
            IsRequired = request.IsRequired,
        };
        _db.ConfigTypes.Add(ct);
        await _db.SaveChangesAsync();
        return _mapper.Map<ConfigTypeDto>(ct);
    }

    public async Task<ConfigTypeDto> UpdateAsync(Guid id, UpdateConfigTypeRequest request)
    {
        var ct = await _db.ConfigTypes.FindAsync(id)
            ?? throw new NotFoundException($"ConfigType {id} not found.");
        ct.Name = request.Name;
        ct.DisplayOrder = request.DisplayOrder;
        ct.IsRequired = request.IsRequired;
        ct.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return _mapper.Map<ConfigTypeDto>(ct);
    }

    public async Task DeleteAsync(Guid id)
    {
        var ct = await _db.ConfigTypes.FindAsync(id)
            ?? throw new NotFoundException($"ConfigType {id} not found.");
        _db.ConfigTypes.Remove(ct);
        await _db.SaveChangesAsync();
    }
}
