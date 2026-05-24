using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TechStock.Application.DTOs.Brands;
using TechStock.Application.Exceptions;
using TechStock.Domain.Entities;
using TechStock.Infrastructure.Data;

namespace TechStock.Infrastructure.Services;

public interface IBrandService
{
    Task<List<BrandDto>> GetAllAsync();
    Task<BrandDto?> GetByIdAsync(Guid id);
    Task<BrandDto> CreateAsync(CreateBrandRequest request);
    Task<BrandDto> UpdateAsync(Guid id, UpdateBrandRequest request);
    Task DeleteAsync(Guid id);
}

public class BrandService : IBrandService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public BrandService(AppDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<List<BrandDto>> GetAllAsync() =>
        _mapper.Map<List<BrandDto>>(await _db.Brands.OrderBy(b => b.Name).ToListAsync());

    public async Task<BrandDto?> GetByIdAsync(Guid id) =>
        _mapper.Map<BrandDto>(await _db.Brands.FindAsync(id));

    public async Task<BrandDto> CreateAsync(CreateBrandRequest request)
    {
        var brand = new Brand { Name = request.Name, Country = request.Country };
        _db.Brands.Add(brand);
        await _db.SaveChangesAsync();
        return _mapper.Map<BrandDto>(brand);
    }

    public async Task<BrandDto> UpdateAsync(Guid id, UpdateBrandRequest request)
    {
        var brand = await _db.Brands.FindAsync(id)
            ?? throw new NotFoundException($"Brand {id} not found.");
        brand.Name = request.Name;
        brand.Country = request.Country;
        brand.IsActive = request.IsActive;
        brand.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return _mapper.Map<BrandDto>(brand);
    }

    public async Task DeleteAsync(Guid id)
    {
        var brand = await _db.Brands.FindAsync(id)
            ?? throw new NotFoundException($"Brand {id} not found.");
        brand.IsActive = false;
        await _db.SaveChangesAsync();
    }
}
