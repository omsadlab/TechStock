namespace TechStock.Application.DTOs.Brands;

public record BrandDto(Guid Id, string Name, string? Country, bool IsActive);

public record CreateBrandRequest(string Name, string? Country);

public record UpdateBrandRequest(string Name, string? Country, bool IsActive);
