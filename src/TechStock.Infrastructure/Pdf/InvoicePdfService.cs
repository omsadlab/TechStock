using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TechStock.Application.DTOs.Reports;
using TechStock.Application.DTOs.Sales;
using TechStock.Application.Interfaces;

namespace TechStock.Infrastructure.Pdf;

public class InvoicePdfService : IInvoicePdfService
{
    // Palette
    private const string Navy    = "#1B3A5C";
    private const string NavyFg  = "#9DB8D4";   // light text on navy bg
    private const string LightBg = "#EEF2F7";   // alternating row / info box
    private const string GrayBdr = "#E2E8F0";   // dividers
    private const string GrayTxt = "#64748B";   // secondary text
    private const string BodyTxt = "#1E293B";   // primary body text
    private const string White   = "#FFFFFF";

    public byte[] GenerateInvoice(SaleDto sale, ShopSettingsDto shop)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(BodyTxt));

                page.Content().Column(col =>
                {
                    // ── Header band ──────────────────────────────────────────
                    col.Item()
                       .Background(Navy)
                       .PaddingVertical(28).PaddingHorizontal(40)
                       .Row(row =>
                       {
                           // Shop info (left)
                           row.RelativeItem().Column(c =>
                           {
                               c.Item().Text(shop.ShopName)
                                   .Bold().FontSize(22).FontColor(White);
                               c.Item().PaddingTop(6)
                                   .Text(shop.ShopAddress)
                                   .FontSize(9).FontColor(NavyFg);
                               if (!string.IsNullOrEmpty(shop.ShopPhone))
                                   c.Item().PaddingTop(2)
                                       .Text($"Tel: {shop.ShopPhone}")
                                       .FontSize(9).FontColor(NavyFg);
                               if (!string.IsNullOrEmpty(shop.ShopEmail))
                                   c.Item().PaddingTop(2)
                                       .Text(shop.ShopEmail)
                                       .FontSize(9).FontColor(NavyFg);
                           });

                           // "INVOICE" word-mark (right)
                           row.ConstantItem(130).AlignRight().AlignMiddle()
                              .Text("INVOICE")
                              .Bold().FontSize(26).FontColor(NavyFg);
                       });

                    // ── Customer & invoice details ────────────────────────────
                    col.Item()
                       .PaddingHorizontal(40).PaddingTop(28)
                       .Row(row =>
                       {
                           // Bill To (left)
                           row.RelativeItem().Column(c =>
                           {
                               c.Item().Text("BILL TO")
                                   .Bold().FontSize(8).FontColor(GrayTxt);
                               c.Item().PaddingTop(6)
                                   .Text(sale.CustomerName ?? "Walk-in Customer")
                                   .Bold().FontSize(13).FontColor(BodyTxt);
                               if (!string.IsNullOrEmpty(sale.CustomerPhone))
                                   c.Item().PaddingTop(3)
                                       .Text($"Tel: {sale.CustomerPhone}")
                                       .FontColor(BodyTxt);
                           });

                           // Invoice details box (right)
                           row.ConstantItem(230)
                              .Background(LightBg)
                              .Padding(14)
                              .Column(c =>
                              {
                                  c.Item().PaddingBottom(8).Row(r =>
                                  {
                                      r.ConstantItem(90).Text("Invoice No:")
                                           .FontColor(GrayTxt);
                                      r.RelativeItem().AlignRight()
                                           .Text(sale.InvoiceNumber)
                                           .Bold().FontColor(BodyTxt);
                                  });
                                  c.Item().Row(r =>
                                  {
                                      r.ConstantItem(90).Text("Date:")
                                           .FontColor(GrayTxt);
                                      r.RelativeItem().AlignRight()
                                           .Text(sale.SaleDate.ToString("dd MMM yyyy"))
                                           .Bold().FontColor(BodyTxt);
                                  });
                              });
                       });

                    // ── Divider ───────────────────────────────────────────────
                    col.Item()
                       .PaddingHorizontal(40).PaddingTop(24)
                       .Height(1).Background(GrayBdr);

                    // ── Items table ───────────────────────────────────────────
                    col.Item()
                       .PaddingHorizontal(40).PaddingTop(16)
                       .Table(table =>
                       {
                           // Columns: # | Product | Warranty | Qty | Unit Price | Total
                           // Usable width = A4(595) − 2×40 = 515pt
                           // Fixed: 28+70+44+115+115 = 372  →  relative = 143
                           table.ColumnsDefinition(cols =>
                           {
                               cols.ConstantColumn(28);   // #
                               cols.RelativeColumn();     // Product
                               cols.ConstantColumn(70);   // Warranty
                               cols.ConstantColumn(44);   // Qty
                               cols.ConstantColumn(115);  // Unit Price
                               cols.ConstantColumn(115);  // Total
                           });

                           // Header row
                           table.Header(h =>
                           {
                               void Hdr(string text, bool right = false, bool center = false)
                               {
                                   var cell = h.Cell()
                                               .Background(Navy)
                                               .PaddingVertical(11)
                                               .PaddingHorizontal(6);
                                   if (right)  cell = cell.AlignRight();
                                   if (center) cell = cell.AlignCenter();
                                   cell.Text(text)
                                       .Bold().FontSize(9).FontColor(White);
                               }

                               Hdr("#");
                               Hdr("PRODUCT");
                               Hdr("WARRANTY", center: true);
                               Hdr("QTY", center: true);
                               Hdr("UNIT PRICE", right: true);
                               Hdr("TOTAL", right: true);
                           });

                           // Data rows
                           int i = 1;
                           foreach (var item in sale.Items)
                           {
                               var bg = i % 2 == 0 ? LightBg : White;

                               table.Cell().Background(bg)
                                    .PaddingVertical(10).PaddingLeft(6)
                                    .Text(i.ToString()).FontColor(GrayTxt);

                               table.Cell().Background(bg)
                                    .PaddingVertical(10).PaddingLeft(4)
                                    .Column(c =>
                                    {
                                        c.Item().Text(item.ProductName).FontColor(BodyTxt);
                                        if (!string.IsNullOrEmpty(item.Barcode))
                                            c.Item().PaddingTop(2)
                                                .Text(item.Barcode)
                                                .FontSize(8).FontColor(GrayTxt);
                                    });

                               table.Cell().Background(bg)
                                    .PaddingVertical(10).AlignCenter()
                                    .Text(item.WarrantyMonths.HasValue ? $"{item.WarrantyMonths} mo." : "—")
                                    .FontSize(9).FontColor(item.WarrantyMonths.HasValue ? BodyTxt : GrayTxt);

                               table.Cell().Background(bg)
                                    .PaddingVertical(10).AlignCenter()
                                    .Text(item.Quantity.ToString()).FontColor(BodyTxt);

                               table.Cell().Background(bg)
                                    .PaddingVertical(10).AlignRight().PaddingRight(8)
                                    .Text($"LKR {item.UnitSellingPrice:N0}").FontColor(BodyTxt);

                               table.Cell().Background(bg)
                                    .PaddingVertical(10).AlignRight().PaddingRight(8)
                                    .Text($"LKR {item.LineTotal:N0}").FontColor(BodyTxt);

                               i++;
                           }
                       });

                    // ── Totals ────────────────────────────────────────────────
                    col.Item()
                       .PaddingHorizontal(40).PaddingTop(20)
                       .Row(outerRow =>
                       {
                           outerRow.RelativeItem();   // left spacer

                           outerRow.ConstantItem(310).Column(c =>
                           {
                               // Subtotal row
                               c.Item()
                                .BorderBottom(0.5f).BorderColor(GrayBdr)
                                .PaddingVertical(9).PaddingHorizontal(14)
                                .Row(r =>
                                {
                                    r.RelativeItem().Text("Subtotal").FontColor(GrayTxt);
                                    r.ConstantItem(120).AlignRight()
                                        .Text($"LKR {sale.SubtotalLKR:N0}").FontColor(BodyTxt);
                                });

                               // Discount row
                               c.Item()
                                .BorderBottom(0.5f).BorderColor(GrayBdr)
                                .PaddingVertical(9).PaddingHorizontal(14)
                                .Row(r =>
                                {
                                    r.RelativeItem().Text("Discount").FontColor(GrayTxt);
                                    r.ConstantItem(120).AlignRight()
                                        .Text($"LKR {sale.DiscountLKR:N0}").FontColor(BodyTxt);
                                });

                               // Total highlight row
                               c.Item()
                                .Background(Navy)
                                .PaddingVertical(13).PaddingHorizontal(14)
                                .Row(r =>
                                {
                                    r.RelativeItem()
                                        .Text("TOTAL")
                                        .Bold().FontSize(12).FontColor(White);
                                    r.ConstantItem(120).AlignRight()
                                        .Text($"LKR {sale.TotalLKR:N0}")
                                        .Bold().FontSize(12).FontColor(White);
                                });
                           });
                       });

                    // ── Footer ────────────────────────────────────────────────
                    col.Item()
                       .PaddingHorizontal(40).PaddingTop(40)
                       .BorderTop(0.5f).BorderColor(GrayBdr)
                       .PaddingTop(16)
                       .Column(c =>
                       {
                           if (!string.IsNullOrEmpty(shop.InvoiceFooterNote))
                               c.Item().Text(shop.InvoiceFooterNote)
                                   .Italic().FontColor(GrayTxt);
                           if (!string.IsNullOrEmpty(shop.WarrantyEmail))
                               c.Item().PaddingTop(4)
                                   .Text($"Warranty: {shop.WarrantyEmail}")
                                   .FontSize(9).FontColor(GrayTxt);
                       });
                });
            });
        }).GeneratePdf();
    }
}
