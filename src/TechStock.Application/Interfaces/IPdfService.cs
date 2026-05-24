using TechStock.Application.DTOs.Reports;
using TechStock.Application.DTOs.Sales;

namespace TechStock.Application.Interfaces;

public interface IInvoicePdfService
{
    byte[] GenerateInvoice(SaleDto sale, ShopSettingsDto shop);
}

public interface IReportPdfService
{
    byte[] GenerateProfitReport(ProfitReportDto report, ShopSettingsDto shop);
    byte[] GenerateStockReport(StockOnHandDto stock, ShopSettingsDto shop);
}
