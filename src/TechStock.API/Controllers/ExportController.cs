using ClosedXML.Excel;
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
    private readonly IWarrantyClaimService _claims;

    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public ExportController(IExcelExportService excel, IReportPdfService pdf,
        IReportService reports, ISettingsService settings, IWarrantyClaimService claims)
    {
        _excel = excel;
        _pdf = pdf;
        _reports = reports;
        _settings = settings;
        _claims = claims;
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

    [HttpGet("claims")]
    [Authorize(Policy = "AllRoles")]
    public async Task<IActionResult> Claims([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] Guid? batchId, [FromQuery] Guid? batchItemId,
        [FromQuery] string? batchNumber = null, [FromQuery] string? barcode = null)
    {
        var items = await _claims.GetReportAsync(from, to, batchId, batchItemId, batchNumber, barcode);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Warranty Claims");

        var headers = new[] {
            "Claim#", "Date", "Invoice#", "Customer", "Phone",
            "Product", "Brand", "Barcode", "Batch", "Warranty (mo.)",
            "Type", "Component", "Status", "Resolved Date", "Notes",
            "Replacement Product", "Replacement Brand", "Replacement Barcode", "Replacement Batch", "Replacement Cost (LKR)"
        };
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = headers[c];
            ws.Cell(1, c + 1).Style.Font.Bold = true;
            ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var item in items)
        {
            ws.Cell(row, 1).Value = item.ClaimNumber;
            ws.Cell(row, 2).Value = item.ClaimedAt.ToString("yyyy-MM-dd");
            ws.Cell(row, 3).Value = item.InvoiceNumber;
            ws.Cell(row, 4).Value = item.CustomerName ?? "Walk-in";
            ws.Cell(row, 5).Value = item.CustomerPhone ?? "";
            ws.Cell(row, 6).Value = item.ProductName;
            ws.Cell(row, 7).Value = item.BrandName;
            ws.Cell(row, 8).Value = item.Barcode ?? "";
            ws.Cell(row, 9).Value = item.BatchNumber;
            ws.Cell(row, 10).Value = item.WarrantyMonths.HasValue ? item.WarrantyMonths.Value.ToString() : "—";
            ws.Cell(row, 11).Value = item.ClaimType;
            ws.Cell(row, 12).Value = item.ComponentName ?? "";
            ws.Cell(row, 13).Value = item.Status;
            ws.Cell(row, 14).Value = item.ResolvedAt.HasValue ? item.ResolvedAt.Value.ToString("yyyy-MM-dd") : "";
            ws.Cell(row, 15).Value = item.ResolutionNotes ?? "";
            ws.Cell(row, 16).Value = item.ReplacementProductName ?? "";
            ws.Cell(row, 17).Value = item.ReplacementBrandName ?? "";
            ws.Cell(row, 18).Value = item.ReplacementBarcode ?? "";
            ws.Cell(row, 19).Value = item.ReplacementBatchNumber ?? "";
            if (item.ReplacementCostLKR.HasValue)
            {
                ws.Cell(row, 20).Value = item.ReplacementCostLKR.Value;
                ws.Cell(row, 20).Style.NumberFormat.Format = "#,##0.00";
            }
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), XlsxMime, $"warranty_claims_{DateTime.UtcNow:yyyyMMdd}.xlsx");
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
