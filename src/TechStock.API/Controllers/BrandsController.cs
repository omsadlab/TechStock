using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStock.Application.DTOs.Brands;
using TechStock.Infrastructure.Services;

namespace TechStock.API.Controllers;

[ApiController]
[Route("api/brands")]
[Authorize]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _brands;

    public BrandsController(IBrandService brands) => _brands = brands;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _brands.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var brand = await _brands.GetByIdAsync(id);
        return brand == null ? NotFound() : Ok(brand);
    }

    [HttpPost, Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateBrandRequest request)
    {
        var brand = await _brands.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = brand.Id }, brand);
    }

    [HttpPut("{id:guid}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBrandRequest request) =>
        Ok(await _brands.UpdateAsync(id, request));

    [HttpDelete("{id:guid}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _brands.DeleteAsync(id);
        return NoContent();
    }
}
