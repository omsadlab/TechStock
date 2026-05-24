using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStock.Application.DTOs.Products;
using TechStock.Application.Interfaces;

namespace TechStock.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProductQueryParams query)
    {
        var includeCost = User.IsInRole("Admin") || User.IsInRole("Manager");
        return Ok(await _service.GetProductsAsync(query, includeCost));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var includeCost = User.IsInRole("Admin") || User.IsInRole("Manager");
        var product = await _service.GetByIdAsync(id, includeStock: true, includeCost);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpGet("{id:guid}/stock")]
    public async Task<IActionResult> GetStock(Guid id) =>
        Ok(new { totalStock = await _service.GetTotalStockAsync(id) });

    [HttpPost, Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var product = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}"), Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request) =>
        Ok(await _service.UpdateAsync(id, request));

    [HttpDelete("{id:guid}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
