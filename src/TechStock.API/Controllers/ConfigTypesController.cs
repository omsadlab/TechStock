using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStock.Application.DTOs.Products;
using TechStock.Application.Interfaces;

namespace TechStock.API.Controllers;

[ApiController]
[Route("api/config-types")]
[Authorize]
public class ConfigTypesController : ControllerBase
{
    private readonly IConfigTypeService _service;

    public ConfigTypesController(IConfigTypeService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetByProductType([FromQuery] Guid productTypeId) =>
        Ok(await _service.GetByProductTypeAsync(productTypeId));

    [HttpPost, Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateConfigTypeRequest request) =>
        Ok(await _service.CreateAsync(request));

    [HttpPut("{id:guid}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateConfigTypeRequest request) =>
        Ok(await _service.UpdateAsync(id, request));

    [HttpDelete("{id:guid}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
