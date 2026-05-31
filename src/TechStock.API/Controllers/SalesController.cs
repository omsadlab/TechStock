using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStock.Application.DTOs.Sales;
using TechStock.Application.Interfaces;

namespace TechStock.API.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ISaleService _sales;
    private readonly IReportService _reports;
    private readonly IInvoicePdfService _invoicePdf;
    private readonly ISettingsService _settings;

    public SalesController(ISaleService sales, IReportService reports,
        IInvoicePdfService invoicePdf, ISettingsService settings)
    {
        _sales = sales;
        _reports = reports;
        _invoicePdf = invoicePdf;
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] SaleQueryParams query)
    {
        Guid? userId = User.IsInRole("Salesperson") ? GetUserId() : null;
        return Ok(await _sales.GetSalesAsync(query, userId));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var sale = await _sales.GetByIdAsync(id);
        if (sale == null) return NotFound();

        if (User.IsInRole("Salesperson") && sale.CreatedBy != GetUserId())
            return Forbid();

        return Ok(sale);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleRequest request)
    {
        var sale = await _sales.CreateAsync(request, GetUserId());
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
    }

    [HttpPut("{id:guid}"), Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSaleRequest request)
    {
        await _sales.UpdateSaleAsync(id, request);
        return NoContent();
    }

    [HttpPut("{id:guid}/items/{itemId:guid}"), Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> UpdateItem(Guid id, Guid itemId, [FromBody] UpdateSaleItemRequest request)
    {
        await _sales.UpdateSaleItemAsync(id, itemId, request);
        return NoContent();
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}"), Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId)
    {
        await _sales.RemoveSaleItemAsync(id, itemId);
        return NoContent();
    }

    [HttpPost("{id:guid}/items"), Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] CreateSaleItemRequest request)
    {
        var sale = await _sales.AddSaleItemAsync(id, request);
        return Ok(sale);
    }

    [HttpGet("{id:guid}/invoice")]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        var sale = await _sales.GetByIdAsync(id);
        if (sale == null) return NotFound();

        if (User.IsInRole("Salesperson") && sale.CreatedBy != GetUserId())
            return Forbid();

        var shopSettings = await GetShopSettingsAsync();
        var pdf = _invoicePdf.GenerateInvoice(sale, shopSettings);

        return File(pdf, "application/pdf", $"invoice-{sale.InvoiceNumber}.pdf");
    }

    [HttpGet("{id:guid}/excel")]
    public async Task<IActionResult> GetExcel(Guid id)
    {
        var sale = await _sales.GetByIdAsync(id);
        if (sale == null) return NotFound();

        if (User.IsInRole("Salesperson") && sale.CreatedBy != GetUserId())
            return Forbid();

        var bytes = await _sales.ExportSaleExcelAsync(id);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"sale-{sale.InvoiceNumber}.xlsx");
    }

    private async Task<Application.DTOs.Reports.ShopSettingsDto> GetShopSettingsAsync()
    {
        var settings = await _settings.GetAllAsync();
        var dict = settings.ToDictionary(s => s.Key, s => s.Value);
        return new Application.DTOs.Reports.ShopSettingsDto
        {
            ShopName = dict.GetValueOrDefault("ShopName", "TechStock"),
            ShopAddress = dict.GetValueOrDefault("ShopAddress", ""),
            ShopPhone = dict.GetValueOrDefault("ShopPhone", ""),
            ShopEmail = dict.GetValueOrDefault("ShopEmail", ""),
            InvoiceFooterNote = dict.GetValueOrDefault("InvoiceFooterNote", ""),
            WarrantyEmail = dict.GetValueOrDefault("WarrantyEmail", ""),
        };
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
