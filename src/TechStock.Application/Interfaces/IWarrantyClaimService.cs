using TechStock.Application.DTOs.Claims;
using TechStock.Application.DTOs.Products;

namespace TechStock.Application.Interfaces;

public interface IWarrantyClaimService
{
    Task<PagedResult<WarrantyClaimDto>> GetClaimsAsync(ClaimQueryParams query);
    Task<WarrantyClaimDto?> GetByIdAsync(Guid id);
    Task<WarrantyClaimDto> CreateAsync(CreateWarrantyClaimRequest request, Guid userId);
    Task<WarrantyClaimDto> UpdateStatusAsync(Guid id, UpdateClaimStatusRequest request, Guid userId);
    Task<List<WarrantyClaimDto>> GetReportAsync(DateTime? from, DateTime? to, Guid? batchId, Guid? batchItemId, string? batchNumber = null, string? barcode = null);
    Task<List<ReplacementCandidateDto>> GetReplacementCandidatesAsync(string? search);
}
