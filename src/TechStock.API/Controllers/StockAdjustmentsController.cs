using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStock.Application.DTOs.Sales;
using TechStock.Application.Interfaces;

namespace TechStock.API.Controllers;

[ApiController]
[Route("api/stock-adjustments")]
[Authorize(Policy = "AdminOrManager")]
public class StockAdjustmentsController : ControllerBase
{
    private readonly IStockAdjustmentService _service;

    public StockAdjustmentsController(IStockAdjustmentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetByBatchItem([FromQuery] Guid batchItemId) =>
        Ok(await _service.GetByBatchItemAsync(batchItemId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStockAdjustmentRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        return Ok(await _service.CreateAsync(request, userId));
    }
}
