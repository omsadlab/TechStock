using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TechStock.Application.Interfaces;
using TechStock.Domain.Entities;
using TechStock.Infrastructure.Data;

namespace TechStock.Infrastructure.Excel;

public class ExcelImportService : IExcelImportService
{
    private readonly AppDbContext _db;

    public ExcelImportService(AppDbContext db) => _db = db;

    public async Task<ImportResult> ImportProductsAsync(Stream fileStream, string onDuplicate)
    {
        var result = new ImportResult();
        using var wb = new XLWorkbook(fileStream);
        var ws = wb.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            try
            {
                var name = ws.Cell(row, 1).GetString().Trim();
                var brandName = ws.Cell(row, 2).GetString().Trim();
                var typeName = ws.Cell(row, 3).GetString().Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Skipped++;
                    continue;
                }

                var brand = await _db.Brands.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(b => b.Name.ToLower() == brandName.ToLower());
                if (brand == null) { result.Errors.Add($"Row {row}: Brand '{brandName}' not found."); continue; }

                var productType = await _db.ProductTypes.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == typeName.ToLower());
                if (productType == null) { result.Errors.Add($"Row {row}: ProductType '{typeName}' not found."); continue; }

                var existing = await _db.Products.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower() && p.BrandId == brand.Id);

                if (existing != null)
                {
                    if (onDuplicate == "update")
                    {
                        existing.Model = ws.Cell(row, 4).GetString().Trim();
                        existing.Description = ws.Cell(row, 5).GetString().Trim();
                        existing.UpdatedAt = DateTime.UtcNow;
                        result.Imported++;
                    }
                    else
                    {
                        result.Skipped++;
                    }
                }
                else
                {
                    _db.Products.Add(new Product
                    {
                        Name = name,
                        BrandId = brand.Id,
                        ProductTypeId = productType.Id,
                        Model = ws.Cell(row, 4).GetString().Trim(),
                        Description = ws.Cell(row, 5).GetString().Trim(),
                    });
                    result.Imported++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {row}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync();
        return result;
    }

    public async Task<ImportResult> ImportBatchItemsAsync(Guid batchId, Stream fileStream)
    {
        var result = new ImportResult();
        var batch = await _db.Batches.FindAsync(batchId);
        if (batch == null) { result.Errors.Add("Batch not found."); return result; }

        using var wb = new XLWorkbook(fileStream);
        var ws = wb.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            try
            {
                var productName = ws.Cell(row, 1).GetString().Trim();
                if (string.IsNullOrWhiteSpace(productName)) { result.Skipped++; continue; }

                if (!int.TryParse(ws.Cell(row, 2).GetString(), out var qty) || qty <= 0)
                { result.Errors.Add($"Row {row}: Invalid quantity."); continue; }

                if (!decimal.TryParse(ws.Cell(row, 3).GetString(), out var costJpy) || costJpy <= 0)
                { result.Errors.Add($"Row {row}: Invalid UnitCostJPY."); continue; }

                if (!decimal.TryParse(ws.Cell(row, 4).GetString(), out var sellingPrice) || sellingPrice <= 0)
                { result.Errors.Add($"Row {row}: Invalid SellingPriceLKR."); continue; }

                var product = await _db.Products.FirstOrDefaultAsync(p => p.Name.ToLower() == productName.ToLower());
                if (product == null) { result.Errors.Add($"Row {row}: Product '{productName}' not found."); continue; }

                var unitCostLKR = Math.Round(costJpy * batch.ExchangeRate, 2);
                _db.BatchItems.Add(new BatchItem
                {
                    BatchId = batchId,
                    ProductId = product.Id,
                    Quantity = qty,
                    UnitCostJPY = costJpy,
                    UnitCostLKR = unitCostLKR,
                    SellingPriceLKR = sellingPrice,
                    RemainingQty = qty,
                });
                result.Imported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {row}: {ex.Message}");
            }
        }

        if (result.Imported > 0)
        {
            await _db.SaveChangesAsync();
            var allItems = await _db.BatchItems.Where(bi => bi.BatchId == batchId).ToListAsync();
            batch.TotalCostLKR = allItems.Sum(i => i.UnitCostLKR * i.Quantity);
            await _db.SaveChangesAsync();
        }

        return result;
    }

    public byte[] GetProductsTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Products");
        var headers = new[] { "Name*", "BrandName*", "ProductTypeName*", "Model", "Description" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] GetBatchItemsTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BatchItems");
        var headers = new[] { "ProductName*", "Quantity*", "UnitCostJPY*", "SellingPriceLKR*" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
