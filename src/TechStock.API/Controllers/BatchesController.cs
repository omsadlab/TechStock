using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStock.Application.DTOs.Batches;
using TechStock.Application.Interfaces;

namespace TechStock.API.Controllers;

[ApiController]
[Route("api/batches")]
[Authorize(Policy = "AdminOrManager")]
public class BatchesController : ControllerBase
{
    private readonly IBatchService _service;

    public BatchesController(IBatchService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] BatchQueryParams query) =>
        Ok(await _service.GetBatchesAsync(query));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var batch = await _service.GetByIdAsync(id);
        return batch == null ? NotFound() : Ok(batch);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBatchRequest request)
    {
        var userId = GetUserId();
        var batch = await _service.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = batch.Id }, batch);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBatchRequest request) =>
        Ok(await _service.UpdateAsync(id, request));

    [HttpDelete("{id:guid}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItems(Guid id, [FromBody] List<CreateBatchItemRequest> items) =>
        Ok(await _service.AddItemsAsync(id, items));

    [HttpPut("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, Guid itemId, [FromBody] UpdateBatchItemRequest request) =>
        Ok(await _service.UpdateItemAsync(id, itemId, request));

    [HttpDelete("{id:guid}/items/{itemId:guid}"), Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteItem(Guid id, Guid itemId)
    {
        await _service.DeleteItemAsync(id, itemId);
        return NoContent();
    }

    [HttpPut("{id:guid}/items/{itemId:guid}/selling-price")]
    public async Task<IActionResult> UpdateSellingPrice(Guid id, Guid itemId, [FromBody] UpdateSellingPriceRequest request)
    {
        await _service.UpdateSellingPriceAsync(id, itemId, request.SellingPriceLKR);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
