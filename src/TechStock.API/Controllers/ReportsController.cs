using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStock.Application.Interfaces;

namespace TechStock.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() =>
        Ok(await _reports.GetDashboardAsync());

    [HttpGet("profit"), Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> Profit([FromQuery] DateTime from, [FromQuery] DateTime to) =>
        Ok(await _reports.GetProfitReportAsync(from, to));

    [HttpGet("stock-on-hand"), Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> StockOnHand() =>
        Ok(await _reports.GetStockOnHandAsync());

    [HttpGet("sales-summary"), Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> SalesSummary([FromQuery] DateTime from, [FromQuery] DateTime to) =>
        Ok(await _reports.GetSalesSummaryAsync(from, to));

    [HttpGet("low-stock"), Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> LowStock([FromQuery] int threshold = 5) =>
        Ok(await _reports.GetLowStockAsync(threshold));

    [HttpGet("batch-profit/{batchId:guid}"), Authorize(Policy = "AdminOrManager")]
    public async Task<IActionResult> BatchProfit(Guid batchId) =>
        Ok(await _reports.GetBatchProfitAsync(batchId));
}
