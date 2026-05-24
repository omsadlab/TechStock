using TechStock.Application.DTOs.Reports;

namespace TechStock.Application.Interfaces;

public interface IReportService
{
    Task<DashboardDto> GetDashboardAsync();
    Task<ProfitReportDto> GetProfitReportAsync(DateTime from, DateTime to);
    Task<StockOnHandDto> GetStockOnHandAsync();
    Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime from, DateTime to);
    Task<List<LowStockItemDto>> GetLowStockAsync(int threshold);
    Task<ProfitReportDto> GetBatchProfitAsync(Guid batchId);
}
