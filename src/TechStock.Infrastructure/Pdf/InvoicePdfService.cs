using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TechStock.Application.DTOs.Reports;
using TechStock.Application.DTOs.Sales;
using TechStock.Application.Interfaces;

namespace TechStock.Infrastructure.Pdf;

public class InvoicePdfService : IInvoicePdfService
{
    public byte[] GenerateInvoice(SaleDto sale, ShopSettingsDto shop)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Content().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(shop.ShopName).Bold().FontSize(14);
                            c.Item().Text(shop.ShopAddress);
                            c.Item().Text($"Tel: {shop.ShopPhone}");
                        });
                    });

                    col.Item().LineHorizontal(1);

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text($"Invoice No: {sale.InvoiceNumber}").Bold();
                        row.RelativeItem().Text($"Date: {sale.SaleDate:dd MMM yyyy}");
                    });
                    col.Item().Text($"Customer: {sale.CustomerName ?? "Walk-in Customer"}");
                    if (!string.IsNullOrEmpty(sale.CustomerPhone))
                        col.Item().Text($"Phone: {sale.CustomerPhone}");

                    col.Item().PaddingTop(8).LineHorizontal(0.5f);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(20);
                            cols.RelativeColumn();
                            cols.ConstantColumn(35);
                            cols.ConstantColumn(80);
                            cols.ConstantColumn(80);
                        });

                        table.Header(h =>
                        {
                            foreach (var header in new[] { "#", "Product", "Qty", "Unit Price", "Total" })
                                h.Cell().Text(header).Bold();
                        });

                        int i = 1;
                        foreach (var item in sale.Items)
                        {
                            table.Cell().Text(i++.ToString());
                            table.Cell().Text(item.ProductName);
                            table.Cell().Text(item.Quantity.ToString());
                            table.Cell().AlignRight().Text($"LKR {item.UnitSellingPrice:N0}");
                            table.Cell().AlignRight().Text($"LKR {item.LineTotal:N0}");
                        }
                    });

                    col.Item().LineHorizontal(0.5f);

                    col.Item().AlignRight().Column(c =>
                    {
                        c.Item().Text($"Subtotal:  LKR {sale.SubtotalLKR:N0}");
                        c.Item().Text($"Discount:  LKR {sale.DiscountLKR:N0}");
                        c.Item().Text($"TOTAL:     LKR {sale.TotalLKR:N0}").Bold().FontSize(12);
                    });

                    col.Item().PaddingTop(10).LineHorizontal(1);
                    col.Item().PaddingTop(5).Text(shop.InvoiceFooterNote).Italic();
                    col.Item().Text($"Warranty: {shop.WarrantyEmail}");
                });
            });
        }).GeneratePdf();
    }
}
