using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStock.Application.DTOs.Products;
using TechStock.Application.Interfaces;

namespace TechStock.API.Controllers;

[ApiController]
[Route("api/product-types")]
[Authorize]
public class ProductTypesController : ControllerBase
{
    private readonly IProductTypeService _service;

    public ProductTypesController(IProductTypeService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var pt = await _service.GetByIdAsync(id);
        return pt == null ? NotFound() : Ok(pt);
    }

    [HttpGet("{id:guid}/config-types")]
    public async Task<IActionResult> GetConfigTypes(Guid id) =>
        Ok(await _service.GetConfigTypesAsync(id));

    [HttpPost, Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateProductTypeRequest request)
    {
        var pt = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = pt.Id }, pt);
    }

    [HttpPut("{id:guid}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductTypeRequest request) =>
        Ok(await _service.UpdateAsync(id, request));

    [HttpDelete("{id:guid}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
