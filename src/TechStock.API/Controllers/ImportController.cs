using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStock.Application.Interfaces;

namespace TechStock.API.Controllers;

[ApiController]
[Route("api/import")]
[Authorize(Policy = "AdminOrManager")]
public class ImportController : ControllerBase
{
    private readonly IExcelImportService _import;

    public ImportController(IExcelImportService import) => _import = import;

    [HttpGet("templates/products")]
    public IActionResult ProductsTemplate()
    {
        var bytes = _import.GetProductsTemplate();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "products_template.xlsx");
    }

    [HttpGet("templates/batch-items")]
    public IActionResult BatchItemsTemplate()
    {
        var bytes = _import.GetBatchItemsTemplate();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "batch_items_template.xlsx");
    }

    [HttpPost("products")]
    public async Task<IActionResult> ImportProducts(IFormFile file, [FromQuery] string onDuplicate = "skip")
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");
        await using var stream = file.OpenReadStream();
        var result = await _import.ImportProductsAsync(stream, onDuplicate);
        return Ok(result);
    }

    [HttpPost("batch-items/{batchId:guid}")]
    public async Task<IActionResult> ImportBatchItems(Guid batchId, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");
        await using var stream = file.OpenReadStream();
        var result = await _import.ImportBatchItemsAsync(batchId, stream);
        return Ok(result);
    }
}
