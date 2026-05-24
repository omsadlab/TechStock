using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TechStock.Application.DTOs.Reports;
using TechStock.Application.Interfaces;

namespace TechStock.Infrastructure.Pdf;

public class ReportPdfService : IReportPdfService
{
    public byte[] GenerateProfitReport(ProfitReportDto report, ShopSettingsDto shop)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text(shop.ShopName).Bold().FontSize(14);
                    col.Item().Text($"Profit Report: {report.From:dd MMM yyyy} — {report.To:dd MMM yyyy}");
                    col.Item().Text($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}");
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text($"Revenue: LKR {report.TotalRevenueLKR:N0}").Bold();
                        row.RelativeItem().Text($"Cost: LKR {report.TotalCostLKR:N0}").Bold();
                        row.RelativeItem().Text($"Profit: LKR {report.GrossProfitLKR:N0}").Bold();
                        row.RelativeItem().Text($"Margin: {report.MarginPercent:N1}%").Bold();
                    });
                });

                page.Content().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.ConstantColumn(50);
                        cols.ConstantColumn(90);
                        cols.ConstantColumn(90);
                        cols.ConstantColumn(90);
                        cols.ConstantColumn(60);
                    });

                    table.Header(h =>
                    {
                        foreach (var hdr in new[] { "Product", "Brand", "Type", "Qty", "Revenue (LKR)", "Cost (LKR)", "Profit (LKR)", "Margin %" })
                            h.Cell().Background("#EFEFEF").Text(hdr).Bold();
                    });

                    foreach (var line in report.Lines)
                    {
                        table.Cell().Text(line.ProductName);
                        table.Cell().Text(line.BrandName);
                        table.Cell().Text(line.ProductTypeName);
                        table.Cell().AlignRight().Text(line.QtySold.ToString());
                        table.Cell().AlignRight().Text($"{line.RevenueLKR:N0}");
                        table.Cell().AlignRight().Text($"{line.CostLKR:N0}");
                        table.Cell().AlignRight().Text($"{line.ProfitLKR:N0}");
                        table.Cell().AlignRight().Text($"{line.MarginPercent:N1}%");
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public byte[] GenerateStockReport(StockOnHandDto stock, ShopSettingsDto shop)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text(shop.ShopName).Bold().FontSize(14);
                    col.Item().Text($"Stock on Hand — as of {DateTime.Now:dd MMM yyyy}");
                    col.Item().PaddingTop(4).Text($"Total Stock Value: LKR {stock.TotalStockValueLKR:N0}").Bold();
                });

                page.Content().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.ConstantColumn(50);
                        cols.ConstantColumn(90);
                        cols.ConstantColumn(90);
                    });

                    table.Header(h =>
                    {
                        foreach (var hdr in new[] { "Product", "Brand", "Type", "Batch", "Qty", "Selling Price", "Stock Value" })
                            h.Cell().Background("#EFEFEF").Text(hdr).Bold();
                    });

                    foreach (var line in stock.Lines)
                    {
                        table.Cell().Text(line.ProductName);
                        table.Cell().Text(line.BrandName);
                        table.Cell().Text(line.ProductTypeName);
                        table.Cell().Text(line.BatchNumber);
                        table.Cell().AlignRight().Text(line.RemainingQty.ToString());
                        table.Cell().AlignRight().Text($"{line.SellingPriceLKR:N0}");
                        table.Cell().AlignRight().Text($"{line.StockValueLKR:N0}");
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
