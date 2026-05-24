using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TechStock.Application.Interfaces;
using TechStock.Infrastructure.Data;

namespace TechStock.Infrastructure.Excel;

public class ExcelExportService : IExcelExportService
{
    private readonly AppDbContext _db;

    public ExcelExportService(AppDbContext db) => _db = db;

    public async Task<byte[]> ExportProductsAsync(bool includeCost)
    {
        var products = await _db.Products
            .Include(p => p.Brand)
            .Include(p => p.ProductType)
            .Include(p => p.Configs).ThenInclude(c => c.ConfigType)
            .OrderBy(p => p.Name)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Products");

        var headers = new List<string> { "Name", "Brand", "Product Type", "Model", "Description", "Total Stock", "Selling Price (LKR)" };
        if (includeCost) headers.Add("Purchase Price (LKR)");

        for (int i = 0; i < headers.Count; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var p in products)
        {
            var stock = await _db.BatchItems.Where(bi => bi.ProductId == p.Id && bi.RemainingQty > 0)
                .SumAsync(bi => (int?)bi.RemainingQty) ?? 0;
            var latest = await _db.BatchItems.Where(bi => bi.ProductId == p.Id)
                .OrderByDescending(bi => bi.CreatedAt).FirstOrDefaultAsync();

            ws.Cell(row, 1).Value = p.Name;
            ws.Cell(row, 2).Value = p.Brand.Name;
            ws.Cell(row, 3).Value = p.ProductType.Name;
            ws.Cell(row, 4).Value = p.Model ?? "";
            ws.Cell(row, 5).Value = p.Description ?? "";
            ws.Cell(row, 6).Value = stock;
            ws.Cell(row, 7).Value = (double)(latest?.SellingPriceLKR ?? 0);
            if (includeCost) ws.Cell(row, 8).Value = (double)(latest?.UnitCostLKR ?? 0);
            row++;
        }

        ws.Columns().AdjustToContents();
        return SaveWorkbook(wb);
    }

    public async Task<byte[]> ExportInventoryAsync()
    {
        var items = await _db.BatchItems
            .Include(bi => bi.Product).ThenInclude(p => p.Brand)
            .Include(bi => bi.Product).ThenInclude(p => p.ProductType)
            .Include(bi => bi.Batch)
            .Where(bi => bi.RemainingQty > 0)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Inventory");
        var headers = new[] { "Name", "Brand", "Type", "Total Stock", "Selling Price (LKR)", "Last Batch No", "Last Batch Date", "Batch Cost (LKR)" };
        WriteHeaders(ws, headers);

        int row = 2;
        foreach (var bi in items.OrderBy(i => i.Product.Name))
        {
            ws.Cell(row, 1).Value = bi.Product.Name;
            ws.Cell(row, 2).Value = bi.Product.Brand.Name;
            ws.Cell(row, 3).Value = bi.Product.ProductType.Name;
            ws.Cell(row, 4).Value = bi.RemainingQty;
            ws.Cell(row, 5).Value = (double)bi.SellingPriceLKR;
            ws.Cell(row, 6).Value = bi.Batch.BatchNumber;
            ws.Cell(row, 7).Value = bi.Batch.PurchaseDate.ToString("yyyy-MM-dd");
            ws.Cell(row, 8).Value = (double)(bi.UnitCostLKR * bi.Quantity);
            row++;
        }

        ws.Columns().AdjustToContents();
        return SaveWorkbook(wb);
    }

    public async Task<byte[]> ExportBatchAsync(Guid batchId)
    {
        var batch = await _db.Batches
            .Include(b => b.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Brand)
            .Include(b => b.Items).ThenInclude(i => i.Product).ThenInclude(p => p.ProductType)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null) return Array.Empty<byte>();

        using var wb = new XLWorkbook();

        // Sheet 1 — Summary
        var ws1 = wb.Worksheets.Add("Summary");
        ws1.Cell(1, 1).Value = "Batch Number"; ws1.Cell(1, 2).Value = batch.BatchNumber;
        ws1.Cell(2, 1).Value = "Supplier";     ws1.Cell(2, 2).Value = batch.Supplier;
        ws1.Cell(3, 1).Value = "Purchase Date"; ws1.Cell(3, 2).Value = batch.PurchaseDate.ToString("yyyy-MM-dd");
        ws1.Cell(4, 1).Value = "Exchange Rate"; ws1.Cell(4, 2).Value = (double)batch.ExchangeRate;
        ws1.Cell(5, 1).Value = "Total Cost (LKR)"; ws1.Cell(5, 2).Value = (double)batch.TotalCostLKR;
        ws1.Column(1).Style.Font.Bold = true;

        // Sheet 2 — Items
        var ws2 = wb.Worksheets.Add("Items");
        WriteHeaders(ws2, ["Product", "Brand", "Type", "Qty", "Unit Cost (JPY)", "Unit Cost (LKR)", "Selling Price (LKR)", "Remaining Qty", "Profit/Unit (LKR)"]);
        int row = 2;
        foreach (var item in batch.Items)
        {
            ws2.Cell(row, 1).Value = item.Product.Name;
            ws2.Cell(row, 2).Value = item.Product.Brand.Name;
            ws2.Cell(row, 3).Value = item.Product.ProductType.Name;
            ws2.Cell(row, 4).Value = item.Quantity;
            ws2.Cell(row, 5).Value = (double)item.UnitCostJPY;
            ws2.Cell(row, 6).Value = (double)item.UnitCostLKR;
            ws2.Cell(row, 7).Value = (double)item.SellingPriceLKR;
            ws2.Cell(row, 8).Value = item.RemainingQty;
            ws2.Cell(row, 9).Value = (double)(item.SellingPriceLKR - item.UnitCostLKR);
            row++;
        }
        ws2.Columns().AdjustToContents();

        return SaveWorkbook(wb);
    }

    public async Task<byte[]> ExportSalesAsync(DateTime from, DateTime to)
    {
        var sales = await _db.Sales
            .Include(s => s.Items)
            .Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sales");
        WriteHeaders(ws, ["Invoice No", "Sale Date", "Customer Name", "Customer Phone", "Item Count", "Subtotal (LKR)", "Discount (LKR)", "Total (LKR)"]);

        int row = 2;
        foreach (var s in sales)
        {
            ws.Cell(row, 1).Value = s.InvoiceNumber;
            ws.Cell(row, 2).Value = s.SaleDate.ToString("yyyy-MM-dd");
            ws.Cell(row, 3).Value = s.CustomerName ?? "";
            ws.Cell(row, 4).Value = s.CustomerPhone ?? "";
            ws.Cell(row, 5).Value = s.Items.Count;
            ws.Cell(row, 6).Value = (double)s.SubtotalLKR;
            ws.Cell(row, 7).Value = (double)s.DiscountLKR;
            ws.Cell(row, 8).Value = (double)s.TotalLKR;
            row++;
        }

        ws.Columns().AdjustToContents();
        return SaveWorkbook(wb);
    }

    private static void WriteHeaders(IXLWorksheet ws, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }
    }

    private static byte[] SaveWorkbook(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
