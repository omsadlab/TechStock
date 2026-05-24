using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStock.Application.Interfaces;

namespace TechStock.API.Controllers;

[ApiController]
[Route("api/export")]
[Authorize(Policy = "AdminOrManager")]
public class ExportController : ControllerBase
{
    private readonly IExcelExportService _excel;
    private readonly IReportPdfService _pdf;
    private readonly IReportService _reports;
    private readonly ISettingsService _settings;

    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public ExportController(IExcelExportService excel, IReportPdfService pdf,
        IReportService reports, ISettingsService settings)
    {
        _excel = excel;
        _pdf = pdf;
        _reports = reports;
        _settings = settings;
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products()
    {
        var bytes = await _excel.ExportProductsAsync(includeCost: true);
        return File(bytes, XlsxMime, "products_export.xlsx");
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory()
    {
        var bytes = await _excel.ExportInventoryAsync();
        return File(bytes, XlsxMime, "inventory_export.xlsx");
    }

    [HttpGet("batch/{batchId:guid}")]
    public async Task<IActionResult> Batch(Guid batchId)
    {
        var bytes = await _excel.ExportBatchAsync(batchId);
        return File(bytes, XlsxMime, $"batch_export.xlsx");
    }

    [HttpGet("sales")]
    public async Task<IActionResult> Sales([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var bytes = await _excel.ExportSalesAsync(from, to);
        return File(bytes, XlsxMime, $"sales_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
    }

    [HttpGet("report/profit")]
    public async Task<IActionResult> ProfitReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var report = await _reports.GetProfitReportAsync(from, to);
        var shop = await GetShopSettingsAsync();
        var pdf = _pdf.GenerateProfitReport(report, shop);
        return File(pdf, "application/pdf", $"profit_report.pdf");
    }

    [HttpGet("report/stock")]
    public async Task<IActionResult> StockReport()
    {
        var stock = await _reports.GetStockOnHandAsync();
        var shop = await GetShopSettingsAsync();
        var pdf = _pdf.GenerateStockReport(stock, shop);
        return File(pdf, "application/pdf", "stock_report.pdf");
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
}
