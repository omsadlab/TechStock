using Microsoft.EntityFrameworkCore;
using TechStock.Application.DTOs.Claims;
using TechStock.Application.DTOs.Products;
using TechStock.Application.Exceptions;
using TechStock.Application.Interfaces;
using TechStock.Domain.Entities;
using TechStock.Domain.Enums;
using TechStock.Infrastructure.Data;

namespace TechStock.Infrastructure.Services;

public class WarrantyClaimService : IWarrantyClaimService
{
    private readonly AppDbContext _db;

    public WarrantyClaimService(AppDbContext db) => _db = db;

    public async Task<PagedResult<WarrantyClaimDto>> GetClaimsAsync(ClaimQueryParams query)
    {
        var q = BaseQuery();

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<ClaimStatus>(query.Status, true, out var status))
            q = q.Where(c => c.Status == status);
        if (query.From.HasValue) q = q.Where(c => c.ClaimedAt >= query.From.Value);
        if (query.To.HasValue) q = q.Where(c => c.ClaimedAt < query.To.Value.AddDays(1));
        if (query.BatchId.HasValue) q = q.Where(c => c.SaleItem.BatchItem.BatchId == query.BatchId.Value);
        if (query.BatchItemId.HasValue) q = q.Where(c => c.SaleItem.BatchItemId == query.BatchItemId.Value);
        if (!string.IsNullOrWhiteSpace(query.BatchNumber)) q = q.Where(c => c.SaleItem.BatchItem.Batch.BatchNumber.Contains(query.BatchNumber));
        if (!string.IsNullOrWhiteSpace(query.Barcode)) q = q.Where(c => c.SaleItem.BatchItem.Barcode != null && c.SaleItem.BatchItem.Barcode.Contains(query.Barcode));

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(c => c.ClaimedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<WarrantyClaimDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<WarrantyClaimDto?> GetByIdAsync(Guid id)
    {
        var claim = await BaseQuery().FirstOrDefaultAsync(c => c.Id == id);
        return claim == null ? null : ToDto(claim);
    }

    public async Task<WarrantyClaimDto> CreateAsync(CreateWarrantyClaimRequest request, Guid userId)
    {
        var saleItem = await _db.SaleItems
            .Include(si => si.Sale)
            .Include(si => si.BatchItem)
            .FirstOrDefaultAsync(si => si.Id == request.SaleItemId)
            ?? throw new NotFoundException($"SaleItem {request.SaleItemId} not found.");

        if (!Enum.TryParse<ClaimType>(request.ClaimType, true, out var claimType))
            throw new BusinessException($"Invalid claim type: {request.ClaimType}");

        var claimedAt = request.ClaimedAt?.ToUniversalTime() ?? DateTime.UtcNow;
        var count = await _db.WarrantyClaims.CountAsync(c => c.ClaimedAt.Date == claimedAt.Date);

        var claim = new WarrantyClaim
        {
            ClaimNumber = $"CLM-{claimedAt:yyyyMMdd}-{(count + 1):D3}",
            SaleItemId = request.SaleItemId,
            ClaimType = claimType,
            Status = ClaimStatus.Pending,
            ComponentName = request.ComponentName,
            IssueDescription = request.IssueDescription,
            ClaimedAt = claimedAt,
            CreatedBy = userId,
        };

        _db.WarrantyClaims.Add(claim);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(claim.Id))!;
    }

    public async Task<WarrantyClaimDto> UpdateStatusAsync(Guid id, UpdateClaimStatusRequest request, Guid userId)
    {
        if (!Enum.TryParse<ClaimStatus>(request.Status, true, out var status))
            throw new BusinessException($"Invalid status: {request.Status}");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var claim = await _db.WarrantyClaims.FindAsync(id)
                ?? throw new NotFoundException($"WarrantyClaim {id} not found.");

            claim.Status = status;
            claim.ResolutionNotes = request.ResolutionNotes;
            claim.UpdatedAt = DateTime.UtcNow;

            if (status is ClaimStatus.Resolved or ClaimStatus.Rejected)
            {
                claim.ResolvedAt = DateTime.UtcNow;
                claim.ResolvedBy = userId;
            }

            if (status == ClaimStatus.Resolved && request.ReplacementBatchItemId.HasValue && !claim.StockDeducted)
            {
                var batchItem = await _db.BatchItems.FindAsync(request.ReplacementBatchItemId.Value)
                    ?? throw new NotFoundException("Replacement batch item not found.");
                if (batchItem.RemainingQty < 1)
                    throw new BusinessException("Replacement item is out of stock.");

                batchItem.RemainingQty--;
                batchItem.UpdatedAt = DateTime.UtcNow;

                claim.ReplacementBatchItemId = batchItem.Id;
                claim.ReplacementCostLKR = batchItem.UnitCostLKR;
                claim.StockDeducted = true;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return (await GetByIdAsync(id))!;
    }

    public async Task<List<ReplacementCandidateDto>> GetReplacementCandidatesAsync(string? search)
    {
        var q = _db.BatchItems
            .Include(bi => bi.Product).ThenInclude(p => p.Brand)
            .Include(bi => bi.Batch)
            .Where(bi => bi.RemainingQty > 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(bi =>
                bi.Product.Name.Contains(s) ||
                bi.Product.Brand.Name.Contains(s) ||
                (bi.Barcode != null && bi.Barcode.Contains(s)) ||
                bi.Batch.BatchNumber.Contains(s));
        }

        var items = await q.OrderBy(bi => bi.Product.Name).Take(30).ToListAsync();
        return items.Select(bi => new ReplacementCandidateDto
        {
            BatchItemId = bi.Id,
            ProductName = bi.Product.Name,
            BrandName = bi.Product.Brand.Name,
            Barcode = bi.Barcode,
            BatchNumber = bi.Batch.BatchNumber,
            RemainingQty = bi.RemainingQty,
            UnitCostLKR = bi.UnitCostLKR,
            SellingPriceLKR = bi.SellingPriceLKR,
        }).ToList();
    }

    public async Task<List<WarrantyClaimDto>> GetReportAsync(DateTime? from, DateTime? to, Guid? batchId, Guid? batchItemId, string? batchNumber = null, string? barcode = null)
    {
        var q = BaseQuery();
        if (from.HasValue) q = q.Where(c => c.ClaimedAt >= from.Value);
        if (to.HasValue) q = q.Where(c => c.ClaimedAt < to.Value.AddDays(1));
        if (batchId.HasValue) q = q.Where(c => c.SaleItem.BatchItem.BatchId == batchId.Value);
        if (batchItemId.HasValue) q = q.Where(c => c.SaleItem.BatchItemId == batchItemId.Value);
        if (!string.IsNullOrWhiteSpace(batchNumber)) q = q.Where(c => c.SaleItem.BatchItem.Batch.BatchNumber.Contains(batchNumber));
        if (!string.IsNullOrWhiteSpace(barcode)) q = q.Where(c => c.SaleItem.BatchItem.Barcode != null && c.SaleItem.BatchItem.Barcode.Contains(barcode));

        var items = await q.OrderByDescending(c => c.ClaimedAt).ToListAsync();
        return items.Select(ToDto).ToList();
    }

    private IQueryable<WarrantyClaim> BaseQuery() =>
        _db.WarrantyClaims
            .Include(c => c.SaleItem).ThenInclude(si => si.Sale)
            .Include(c => c.SaleItem).ThenInclude(si => si.BatchItem).ThenInclude(bi => bi.Product).ThenInclude(p => p.Brand)
            .Include(c => c.SaleItem).ThenInclude(si => si.BatchItem).ThenInclude(bi => bi.Batch)
            .Include(c => c.ReplacementBatchItem).ThenInclude(bi => bi!.Product).ThenInclude(p => p.Brand)
            .Include(c => c.ReplacementBatchItem).ThenInclude(bi => bi!.Batch);

    private static WarrantyClaimDto ToDto(WarrantyClaim c) => new()
    {
        Id = c.Id,
        ClaimNumber = c.ClaimNumber,
        ClaimedAt = c.ClaimedAt,
        SaleId = c.SaleItem.SaleId,
        InvoiceNumber = c.SaleItem.Sale.InvoiceNumber,
        CustomerName = c.SaleItem.Sale.CustomerName,
        CustomerPhone = c.SaleItem.Sale.CustomerPhone,
        SaleItemId = c.SaleItemId,
        ProductName = c.SaleItem.BatchItem.Product.Name,
        BrandName = c.SaleItem.BatchItem.Product.Brand.Name,
        Barcode = c.SaleItem.BatchItem.Barcode,
        BatchNumber = c.SaleItem.BatchItem.Batch.BatchNumber,
        WarrantyMonths = c.SaleItem.WarrantyMonths,
        ClaimType = c.ClaimType.ToString(),
        Status = c.Status.ToString(),
        ComponentName = c.ComponentName,
        IssueDescription = c.IssueDescription,
        ResolvedAt = c.ResolvedAt,
        ResolutionNotes = c.ResolutionNotes,
        CreatedAt = c.CreatedAt,
        ReplacementBatchItemId = c.ReplacementBatchItemId,
        ReplacementProductName = c.ReplacementBatchItem?.Product.Name,
        ReplacementBrandName = c.ReplacementBatchItem?.Product.Brand.Name,
        ReplacementBarcode = c.ReplacementBatchItem?.Barcode,
        ReplacementBatchNumber = c.ReplacementBatchItem?.Batch.BatchNumber,
        ReplacementCostLKR = c.ReplacementCostLKR,
        StockDeducted = c.StockDeducted,
    };
}
