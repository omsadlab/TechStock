using System.Globalization;
using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TechStock.Domain.Entities;
using TechStock.Domain.Enums;
using TechStock.Infrastructure.Data;

namespace TechStock.API.Controllers;

[ApiController]
[Route("api/admin/backup")]
[Authorize(Policy = "AdminOnly")]
public class BackupController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public BackupController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ── GET /api/admin/backup/data-export ────────────────────────────────────

    [HttpGet("data-export")]
    public async Task<IActionResult> GetDataExport()
    {
        using var wb = new XLWorkbook();

        // Summary sheet (filled at end)
        var wsSummary = wb.Worksheets.Add("Summary");
        wsSummary.Cell(1, 1).Value = "TechStock Data Export";
        wsSummary.Cell(1, 1).Style.Font.Bold = true;
        wsSummary.Cell(2, 1).Value = $"Exported At: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        wsSummary.Cell(4, 1).Value = "Table"; wsSummary.Cell(4, 1).Style.Font.Bold = true;
        wsSummary.Cell(4, 2).Value = "Records"; wsSummary.Cell(4, 2).Style.Font.Bold = true;
        int summaryRow = 5;

        // Brands
        var brands = await _db.Brands.IgnoreQueryFilters().OrderBy(b => b.Name).ToListAsync();
        var wsBrands = wb.Worksheets.Add("Brands");
        SetHeaders(wsBrands, "Id", "Name", "Country", "IsActive", "CreatedAt");
        for (int i = 0; i < brands.Count; i++)
        {
            var b = brands[i]; int r = i + 2;
            wsBrands.Cell(r, 1).Value = b.Id.ToString();
            wsBrands.Cell(r, 2).Value = b.Name;
            wsBrands.Cell(r, 3).Value = b.Country ?? "";
            wsBrands.Cell(r, 4).Value = b.IsActive.ToString();
            wsBrands.Cell(r, 5).Value = b.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        }
        wsSummary.Cell(summaryRow, 1).Value = "Brands"; wsSummary.Cell(summaryRow++, 2).Value = brands.Count;

        // ProductTypes
        var productTypes = await _db.ProductTypes.IgnoreQueryFilters().OrderBy(pt => pt.Name).ToListAsync();
        var wsPt = wb.Worksheets.Add("ProductTypes");
        SetHeaders(wsPt, "Id", "Name", "IsActive", "CreatedAt");
        for (int i = 0; i < productTypes.Count; i++)
        {
            var pt = productTypes[i]; int r = i + 2;
            wsPt.Cell(r, 1).Value = pt.Id.ToString();
            wsPt.Cell(r, 2).Value = pt.Name;
            wsPt.Cell(r, 3).Value = pt.IsActive.ToString();
            wsPt.Cell(r, 4).Value = pt.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        }
        wsSummary.Cell(summaryRow, 1).Value = "ProductTypes"; wsSummary.Cell(summaryRow++, 2).Value = productTypes.Count;

        // ConfigTypes
        var configTypes = await _db.ConfigTypes.IgnoreQueryFilters()
            .OrderBy(ct => ct.ProductTypeId).ThenBy(ct => ct.DisplayOrder).ToListAsync();
        var wsCt = wb.Worksheets.Add("ConfigTypes");
        SetHeaders(wsCt, "Id", "ProductTypeId", "Name", "IsRequired", "DisplayOrder", "CreatedAt");
        for (int i = 0; i < configTypes.Count; i++)
        {
            var ct = configTypes[i]; int r = i + 2;
            wsCt.Cell(r, 1).Value = ct.Id.ToString();
            wsCt.Cell(r, 2).Value = ct.ProductTypeId.ToString();
            wsCt.Cell(r, 3).Value = ct.Name;
            wsCt.Cell(r, 4).Value = ct.IsRequired.ToString();
            wsCt.Cell(r, 5).Value = ct.DisplayOrder;
            wsCt.Cell(r, 6).Value = ct.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        }
        wsSummary.Cell(summaryRow, 1).Value = "ConfigTypes"; wsSummary.Cell(summaryRow++, 2).Value = configTypes.Count;

        // Products
        var products = await _db.Products.IgnoreQueryFilters().OrderBy(p => p.Name).ToListAsync();
        var wsProducts = wb.Worksheets.Add("Products");
        SetHeaders(wsProducts, "Id", "BrandId", "ProductTypeId", "Name", "Model", "Description", "IsActive", "CreatedAt");
        for (int i = 0; i < products.Count; i++)
        {
            var p = products[i]; int r = i + 2;
            wsProducts.Cell(r, 1).Value = p.Id.ToString();
            wsProducts.Cell(r, 2).Value = p.BrandId.ToString();
            wsProducts.Cell(r, 3).Value = p.ProductTypeId.ToString();
            wsProducts.Cell(r, 4).Value = p.Name;
            wsProducts.Cell(r, 5).Value = p.Model ?? "";
            wsProducts.Cell(r, 6).Value = p.Description ?? "";
            wsProducts.Cell(r, 7).Value = p.IsActive.ToString();
            wsProducts.Cell(r, 8).Value = p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        }
        wsSummary.Cell(summaryRow, 1).Value = "Products"; wsSummary.Cell(summaryRow++, 2).Value = products.Count;

        // ProductConfigs
        var configs = await _db.ProductConfigs.IgnoreQueryFilters().ToListAsync();
        var wsPc = wb.Worksheets.Add("ProductConfigs");
        SetHeaders(wsPc, "Id", "ProductId", "ConfigTypeId", "Value");
        for (int i = 0; i < configs.Count; i++)
        {
            var pc = configs[i]; int r = i + 2;
            wsPc.Cell(r, 1).Value = pc.Id.ToString();
            wsPc.Cell(r, 2).Value = pc.ProductId.ToString();
            wsPc.Cell(r, 3).Value = pc.ConfigTypeId.ToString();
            wsPc.Cell(r, 4).Value = pc.Value;
        }
        wsSummary.Cell(summaryRow, 1).Value = "ProductConfigs"; wsSummary.Cell(summaryRow++, 2).Value = configs.Count;

        // Batches
        var batches = await _db.Batches.OrderBy(b => b.PurchaseDate).ToListAsync();
        var wsBatches = wb.Worksheets.Add("Batches");
        SetHeaders(wsBatches, "Id", "BatchNumber", "PurchaseDate", "Supplier", "Currency",
            "ExchangeRate", "TotalCostLKR", "EstimatedArrival", "Notes", "CreatedAt");
        for (int i = 0; i < batches.Count; i++)
        {
            var b = batches[i]; int r = i + 2;
            wsBatches.Cell(r, 1).Value = b.Id.ToString();
            wsBatches.Cell(r, 2).Value = b.BatchNumber;
            wsBatches.Cell(r, 3).Value = b.PurchaseDate.ToString("yyyy-MM-dd HH:mm:ss");
            wsBatches.Cell(r, 4).Value = b.Supplier;
            wsBatches.Cell(r, 5).Value = b.Currency;
            wsBatches.Cell(r, 6).Value = b.ExchangeRate.ToString("F6", CultureInfo.InvariantCulture);
            wsBatches.Cell(r, 7).Value = b.TotalCostLKR.ToString("F2", CultureInfo.InvariantCulture);
            if (b.EstimatedArrival.HasValue) wsBatches.Cell(r, 8).Value = b.EstimatedArrival.Value.ToString("yyyy-MM-dd");
            wsBatches.Cell(r, 9).Value = b.Notes ?? "";
            wsBatches.Cell(r, 10).Value = b.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        }
        wsSummary.Cell(summaryRow, 1).Value = "Batches"; wsSummary.Cell(summaryRow++, 2).Value = batches.Count;

        // BatchItems
        var batchItems = await _db.BatchItems.OrderBy(bi => bi.CreatedAt).ToListAsync();
        var wsBi = wb.Worksheets.Add("BatchItems");
        SetHeaders(wsBi, "Id", "BatchId", "ProductId", "Barcode", "Quantity", "RemainingQty",
            "UnitCostJPY", "UnitCostLKR", "SellingPriceLKR", "CreatedAt");
        for (int i = 0; i < batchItems.Count; i++)
        {
            var bi = batchItems[i]; int r = i + 2;
            wsBi.Cell(r, 1).Value = bi.Id.ToString();
            wsBi.Cell(r, 2).Value = bi.BatchId.ToString();
            wsBi.Cell(r, 3).Value = bi.ProductId.ToString();
            wsBi.Cell(r, 4).Value = bi.Barcode ?? "";
            wsBi.Cell(r, 5).Value = bi.Quantity;
            wsBi.Cell(r, 6).Value = bi.RemainingQty;
            wsBi.Cell(r, 7).Value = bi.UnitCostJPY.ToString("F4", CultureInfo.InvariantCulture);
            wsBi.Cell(r, 8).Value = bi.UnitCostLKR.ToString("F4", CultureInfo.InvariantCulture);
            wsBi.Cell(r, 9).Value = bi.SellingPriceLKR.ToString("F2", CultureInfo.InvariantCulture);
            wsBi.Cell(r, 10).Value = bi.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        }
        wsSummary.Cell(summaryRow, 1).Value = "BatchItems"; wsSummary.Cell(summaryRow++, 2).Value = batchItems.Count;

        // BatchItemWarrantyOptions
        var warrantyOpts = await _db.BatchItemWarrantyOptions.OrderBy(w => w.BatchItemId).ToListAsync();
        var wsWo = wb.Worksheets.Add("WarrantyOptions");
        SetHeaders(wsWo, "Id", "BatchItemId", "WarrantyMonths", "SellingPriceLKR", "IsDefault");
        for (int i = 0; i < warrantyOpts.Count; i++)
        {
            var w = warrantyOpts[i]; int r = i + 2;
            wsWo.Cell(r, 1).Value = w.Id.ToString();
            wsWo.Cell(r, 2).Value = w.BatchItemId.ToString();
            wsWo.Cell(r, 3).Value = w.WarrantyMonths;
            wsWo.Cell(r, 4).Value = w.SellingPriceLKR.ToString("F2", CultureInfo.InvariantCulture);
            wsWo.Cell(r, 5).Value = w.IsDefault.ToString();
        }
        wsSummary.Cell(summaryRow, 1).Value = "WarrantyOptions"; wsSummary.Cell(summaryRow++, 2).Value = warrantyOpts.Count;

        // Sales
        var sales = await _db.Sales.OrderBy(s => s.SaleDate).ToListAsync();
        var wsSales = wb.Worksheets.Add("Sales");
        SetHeaders(wsSales, "Id", "InvoiceNumber", "SaleDate", "CustomerName", "CustomerPhone",
            "SubtotalLKR", "DiscountLKR", "TotalLKR", "CreatedAt");
        for (int i = 0; i < sales.Count; i++)
        {
            var s = sales[i]; int r = i + 2;
            wsSales.Cell(r, 1).Value = s.Id.ToString();
            wsSales.Cell(r, 2).Value = s.InvoiceNumber;
            wsSales.Cell(r, 3).Value = s.SaleDate.ToString("yyyy-MM-dd HH:mm:ss");
            wsSales.Cell(r, 4).Value = s.CustomerName ?? "";
            wsSales.Cell(r, 5).Value = s.CustomerPhone ?? "";
            wsSales.Cell(r, 6).Value = s.SubtotalLKR.ToString("F2", CultureInfo.InvariantCulture);
            wsSales.Cell(r, 7).Value = s.DiscountLKR.ToString("F2", CultureInfo.InvariantCulture);
            wsSales.Cell(r, 8).Value = s.TotalLKR.ToString("F2", CultureInfo.InvariantCulture);
            wsSales.Cell(r, 9).Value = s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        }
        wsSummary.Cell(summaryRow, 1).Value = "Sales"; wsSummary.Cell(summaryRow++, 2).Value = sales.Count;

        // SaleItems
        var saleItems = await _db.SaleItems.OrderBy(si => si.SaleId).ToListAsync();
        var wsSi = wb.Worksheets.Add("SaleItems");
        SetHeaders(wsSi, "Id", "SaleId", "BatchItemId", "Quantity", "UnitSellingPrice",
            "Discount", "LineTotal", "WarrantyMonths");
        for (int i = 0; i < saleItems.Count; i++)
        {
            var si = saleItems[i]; int r = i + 2;
            wsSi.Cell(r, 1).Value = si.Id.ToString();
            wsSi.Cell(r, 2).Value = si.SaleId.ToString();
            wsSi.Cell(r, 3).Value = si.BatchItemId.ToString();
            wsSi.Cell(r, 4).Value = si.Quantity;
            wsSi.Cell(r, 5).Value = si.UnitSellingPrice.ToString("F2", CultureInfo.InvariantCulture);
            wsSi.Cell(r, 6).Value = si.Discount.ToString("F2", CultureInfo.InvariantCulture);
            wsSi.Cell(r, 7).Value = si.LineTotal.ToString("F2", CultureInfo.InvariantCulture);
            if (si.WarrantyMonths.HasValue) wsSi.Cell(r, 8).Value = si.WarrantyMonths.Value;
        }
        wsSummary.Cell(summaryRow, 1).Value = "SaleItems"; wsSummary.Cell(summaryRow++, 2).Value = saleItems.Count;

        // WarrantyClaims
        var claims = await _db.WarrantyClaims.OrderBy(c => c.ClaimedAt).ToListAsync();
        var wsWc = wb.Worksheets.Add("WarrantyClaims");
        SetHeaders(wsWc, "Id", "ClaimNumber", "SaleItemId", "ClaimType", "Status",
            "ComponentName", "IssueDescription", "ClaimedAt", "ResolvedAt", "ResolutionNotes",
            "ReplacementBatchItemId", "ReplacementCostLKR", "StockDeducted", "CreatedAt");
        for (int i = 0; i < claims.Count; i++)
        {
            var c = claims[i]; int r = i + 2;
            wsWc.Cell(r, 1).Value = c.Id.ToString();
            wsWc.Cell(r, 2).Value = c.ClaimNumber;
            wsWc.Cell(r, 3).Value = c.SaleItemId.ToString();
            wsWc.Cell(r, 4).Value = c.ClaimType.ToString();
            wsWc.Cell(r, 5).Value = c.Status.ToString();
            wsWc.Cell(r, 6).Value = c.ComponentName ?? "";
            wsWc.Cell(r, 7).Value = c.IssueDescription;
            wsWc.Cell(r, 8).Value = c.ClaimedAt.ToString("yyyy-MM-dd HH:mm:ss");
            if (c.ResolvedAt.HasValue) wsWc.Cell(r, 9).Value = c.ResolvedAt.Value.ToString("yyyy-MM-dd HH:mm:ss");
            wsWc.Cell(r, 10).Value = c.ResolutionNotes ?? "";
            if (c.ReplacementBatchItemId.HasValue) wsWc.Cell(r, 11).Value = c.ReplacementBatchItemId.Value.ToString();
            if (c.ReplacementCostLKR.HasValue) wsWc.Cell(r, 12).Value = c.ReplacementCostLKR.Value.ToString("F4", CultureInfo.InvariantCulture);
            wsWc.Cell(r, 13).Value = c.StockDeducted.ToString();
            wsWc.Cell(r, 14).Value = c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        }
        wsSummary.Cell(summaryRow, 1).Value = "WarrantyClaims"; wsSummary.Cell(summaryRow++, 2).Value = claims.Count;

        // StockAdjustments
        var adjustments = await _db.StockAdjustments.OrderBy(a => a.CreatedAt).ToListAsync();
        var wsAdj = wb.Worksheets.Add("StockAdjustments");
        SetHeaders(wsAdj, "Id", "BatchItemId", "Type", "QuantityChange", "Reason", "CreatedAt");
        for (int i = 0; i < adjustments.Count; i++)
        {
            var a = adjustments[i]; int r = i + 2;
            wsAdj.Cell(r, 1).Value = a.Id.ToString();
            wsAdj.Cell(r, 2).Value = a.BatchItemId.ToString();
            wsAdj.Cell(r, 3).Value = a.Type.ToString();
            wsAdj.Cell(r, 4).Value = a.QuantityChange;
            wsAdj.Cell(r, 5).Value = a.Reason;
            wsAdj.Cell(r, 6).Value = a.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        }
        wsSummary.Cell(summaryRow, 1).Value = "StockAdjustments"; wsSummary.Cell(summaryRow++, 2).Value = adjustments.Count;

        // Settings
        var settings = await _db.AppSettings.OrderBy(s => s.Key).ToListAsync();
        var wsSettings = wb.Worksheets.Add("Settings");
        SetHeaders(wsSettings, "Key", "Value");
        for (int i = 0; i < settings.Count; i++)
        {
            wsSettings.Cell(i + 2, 1).Value = settings[i].Key;
            wsSettings.Cell(i + 2, 2).Value = settings[i].Value;
        }
        wsSummary.Cell(summaryRow, 1).Value = "Settings"; wsSummary.Cell(summaryRow++, 2).Value = settings.Count;

        foreach (var ws in wb.Worksheets)
            ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"TechStock_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    // ── GET /api/admin/backup/sql ─────────────────────────────────────────────

    [HttpGet("sql")]
    public async Task<IActionResult> GetSqlBackup()
    {
        var connStr = _config.GetConnectionString("Default");
        if (string.IsNullOrEmpty(connStr))
            return BadRequest(new { error = "Connection string not configured." });

        var builder = new SqlConnectionStringBuilder(connStr);
        var dbName = builder.InitialCatalog;
        var tempPath = Path.Combine(Path.GetTempPath(), $"TechStock_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

        try
        {
            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                $"BACKUP DATABASE [{dbName}] TO DISK = @path WITH COMPRESSION, FORMAT, INIT, STATS = 10",
                conn);
            cmd.Parameters.AddWithValue("@path", tempPath);
            cmd.CommandTimeout = 600;
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"SQL backup failed: {ex.Message}. Use Data Export instead for shared/remote SQL Server setups." });
        }

        if (!System.IO.File.Exists(tempPath))
            return BadRequest(new { error = "Backup file not accessible — SQL Server may be on a different machine. Use Data Export instead." });

        try
        {
            var bytes = await System.IO.File.ReadAllBytesAsync(tempPath);
            System.IO.File.Delete(tempPath);
            return File(bytes, "application/octet-stream", $"TechStock_{DateTime.Now:yyyyMMdd}.bak");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Failed to read backup file: {ex.Message}" });
        }
    }

    // ── POST /api/admin/backup/import ─────────────────────────────────────────

    public class ImportFileRequest
    {
        public IFormFile? File { get; set; }
    }

    [HttpPost("import")]
    [RequestSizeLimit(200 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportData([FromForm] ImportFileRequest request)
    {
        var file = request.File;
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        var adminId = GetUserId();
        var result = new ImportResult();

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            using var stream = file.OpenReadStream();
            using var wb = new XLWorkbook(stream);

            // Layer 1: no FK deps
            ImportSettings(wb, result);
            AddBrands(wb, result);
            AddProductTypes(wb, result);
            await _db.SaveChangesAsync();

            // Layer 2: depends on ProductTypes
            AddConfigTypes(wb, result);
            await _db.SaveChangesAsync();

            // Layer 3: depends on Brands + ProductTypes
            AddProducts(wb, result);
            await _db.SaveChangesAsync();

            // Layer 4: depends on Products + ConfigTypes
            AddProductConfigs(wb, result);
            await _db.SaveChangesAsync();

            // Layer 5: Batches (no FK deps beyond user)
            AddBatches(wb, result, adminId);
            await _db.SaveChangesAsync();

            // Layer 6: BatchItems + WarrantyOptions (depends on Batches + Products)
            AddBatchItems(wb, result);
            AddWarrantyOptions(wb, result);
            await _db.SaveChangesAsync();

            // Layer 7: Sales (no FK deps beyond user)
            AddSales(wb, result, adminId);
            await _db.SaveChangesAsync();

            // Layer 8: SaleItems (depends on Sales + BatchItems)
            AddSaleItems(wb, result);
            await _db.SaveChangesAsync();

            // Layer 9: WarrantyClaims (depends on SaleItems + BatchItems)
            AddWarrantyClaims(wb, result, adminId);
            await _db.SaveChangesAsync();

            // Layer 10: StockAdjustments (depends on BatchItems)
            AddStockAdjustments(wb, result, adminId);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return BadRequest(new { error = $"Import failed: {ex.Message}" });
        }

        return Ok(result);
    }

    // ── Import methods ────────────────────────────────────────────────────────

    private void ImportSettings(XLWorkbook wb, ImportResult result)
    {
        var ws = GetSheet(wb, "Settings");
        if (ws == null) return;
        var existing = _db.AppSettings.ToDictionary(s => s.Key, s => s);
        foreach (var row in DataRows(ws))
        {
            var key = row.Cell(1).GetString().Trim();
            var value = row.Cell(2).GetString();
            if (string.IsNullOrEmpty(key)) continue;
            if (existing.TryGetValue(key, out var setting))
                setting.Value = value;
            else
            {
                _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
                result.Settings++;
            }
        }
    }

    private void AddBrands(XLWorkbook wb, ImportResult result)
    {
        var ws = GetSheet(wb, "Brands");
        if (ws == null) return;
        var existing = _db.Brands.IgnoreQueryFilters().Select(b => b.Id).ToHashSet();
        var added = new HashSet<Guid>();
        foreach (var row in DataRows(ws))
        {
            if (!Guid.TryParse(row.Cell(1).GetString(), out var id)) continue;
            if (existing.Contains(id) || added.Contains(id)) continue;
            _db.Brands.Add(new Brand
            {
                Id = id,
                Name = row.Cell(2).GetString().Trim(),
                Country = NullIfEmpty(row.Cell(3).GetString()),
                IsActive = bool.TryParse(row.Cell(4).GetString(), out var a) && a,
                CreatedAt = ParseDt(row.Cell(5).GetString()),
            });
            added.Add(id); result.Brands++;
        }
    }

    private void AddProductTypes(XLWorkbook wb, ImportResult result)
    {
        var ws = GetSheet(wb, "ProductTypes");
        if (ws == null) return;
        var existing = _db.ProductTypes.IgnoreQueryFilters().Select(pt => pt.Id).ToHashSet();
        var added = new HashSet<Guid>();
        foreach (var row in DataRows(ws))
        {
            if (!Guid.TryParse(row.Cell(1).GetString(), out var id)) continue;
            if (existing.Contains(id) || added.Contains(id)) continue;
            _db.ProductTypes.Add(new ProductType
            {
                Id = id,
                Name = row.Cell(2).GetString().Trim(),
                IsActive = bool.TryParse(row.Cell(3).GetString(), out var a) && a,
                CreatedAt = ParseDt(row.Cell(4).GetString()),
            });
            added.Add(id); result.ProductTypes++;
        }
    }

    private void AddConfigTypes(XLWorkbook wb, ImportResult result)
    {
        var ws = GetSheet(wb, "ConfigTypes");
        if (ws == null) return;
        var existing = _db.ConfigTypes.Select(ct => ct.Id).ToHashSet();
        var added = new HashSet<Guid>();
        foreach (var row in DataRows(ws))
        {
            if (!Guid.TryParse(row.Cell(1).GetString(), out var id)) continue;
            if (!Guid.TryParse(row.Cell(2).GetString(), out var ptId)) continue;
            if (existing.Contains(id) || added.Contains(id)) continue;
            _db.ConfigTypes.Add(new ConfigType
            {
                Id = id,
                ProductTypeId = ptId,
                Name = row.Cell(3).GetString().Trim(),
                IsRequired = bool.TryParse(row.Cell(4).GetString(), out var req) && req,
                DisplayOrder = int.TryParse(row.Cell(5).GetString(), out var ord) ? ord : 0,
                CreatedAt = ParseDt(row.Cell(6).GetString()),
            });
            added.Add(id); result.ConfigTypes++;
        }
    }

    private void AddProducts(XLWorkbook wb, ImportResult result)
    {
        var ws = GetSheet(wb, "Products");
        if (ws == null) return;
        var existing = _db.Products.IgnoreQueryFilters().Select(p => p.Id).ToHashSet();
        var added = new HashSet<Guid>();
        foreach (var row in DataRows(ws))
        {
            if (!Guid.TryParse(row.Cell(1).GetString(), out var id)) continue;
            if (!Guid.TryParse(row.Cell(2).GetString(), out var brandId)) continue;
            if (!Guid.TryParse(row.Cell(3).GetString(), out var ptId)) continue;
            if (existing.Contains(id) || added.Contains(id)) continue;
            _db.Products.Add(new Product
            {
                Id = id,
                BrandId = brandId,
                ProductTypeId = ptId,
                Name = row.Cell(4).GetString().Trim(),
                Model = NullIfEmpty(row.Cell(5).GetString()),
                Description = NullIfEmpty(row.Cell(6).GetString()),
                IsActive = bool.TryParse(row.Cell(7).GetString(), out var a) && a,
                CreatedAt = ParseDt(row.Cell(8).GetString()),
            });
            added.Add(id); result.Products++;
        }
    }

    private void AddProductConfigs(XLWorkbook wb, ImportResult result)
    {
        var ws = GetSheet(wb, "ProductConfigs");
        if (ws == null) return;
        var existing = _db.ProductConfigs.Select(pc => pc.Id).ToHashSet();
        var added = new HashSet<Guid>();
        foreach (var row in DataRows(ws))
        {
            if (!Guid.TryParse(row.Cell(1).GetString(), out var id)) continue;
            if (!Guid.TryParse(row.Cell(2).GetString(), out var productId)) continue;
            if (!Guid.TryParse(row.Cell(3).GetString(), out var ctId)) continue;
            if (existing.Contains(id) || added.Contains(id)) continue;
            _db.ProductConfigs.Add(new ProductConfig
            {
                Id = id,
                ProductId = productId,
                ConfigTypeId = ctId,
                Value = row.Cell(4).GetString(),
            });
            added.Add(id); result.ProductConfigs++;
        }
    }

    private void AddBatches(XLWorkbook wb, ImportResult result, Guid adminId)
    {
        var ws = GetSheet(wb, "Batches");
        if (ws == null) return;
        var existing = _db.Batches.Select(b => b.Id).ToHashSet();
        var added = new HashSet<Guid>();
        foreach (var row in DataRows(ws))
        {
            if (!Guid.TryParse(row.Cell(1).GetString(), out var id)) continue;
            if (existing.Contains(id) || added.Contains(id)) continue;
            var arrCell = row.Cell(8);
            _db.Batches.Add(new Batch
            {
                Id = id,
                BatchNumber = row.Cell(2).GetString().Trim(),
                PurchaseDate = ParseDt(row.Cell(3).GetString()),
                Supplier = row.Cell(4).GetString().Trim(),
                Currency = NullIfEmpty(row.Cell(5).GetString()) ?? "JPY",
                ExchangeRate = ParseDecimal(row.Cell(6).GetString()),
                TotalCostLKR = ParseDecimal(row.Cell(7).GetString()),
                EstimatedArrival = arrCell.IsEmpty() ? null : ParseDt(arrCell.GetString()),
                Notes = NullIfEmpty(row.Cell(9).GetString()),
                CreatedAt = ParseDt(row.Cell(10).GetString()),
                CreatedBy = adminId,
            });
            added.Add(id); result.Batches++;
        }
    }

    private void AddBatchItems(XLWorkbook wb, ImportResult result)
    {
        var ws = GetSheet(wb, "BatchItems");
        if (ws == null) return;
        var existing = _db.BatchItems.Select(bi => bi.Id).ToHashSet();
        var added = new HashSet<Guid>();
        foreach (var row in DataRows(ws))
        {
            if (!Guid.TryParse(row.Cell(1).GetString(), out var id)) continue;
            if (!Guid.TryParse(row.Cell(2).GetString(), out var batchId)) continue;
            if (!Guid.TryParse(row.Cell(3).GetString(), out var productId)) continue;
            if (existing.Contains(id) || added.Contains(id)) continue;
            _db.BatchItems.Add(new BatchItem
            {
                Id = id,
                BatchId = batchId,
                ProductId = productId,
                Barcode = NullIfEmpty(row.Cell(4).GetString()),
                Quantity = ParseInt(row.Cell(5).GetString()),
                RemainingQty = ParseInt(row.Cell(6).GetString()),
                UnitCostJPY = ParseDecimal(row.Cell(7).GetString()),
                UnitCostLKR = ParseDecimal(row.Cell(8).GetString()),
                SellingPriceLKR = ParseDecimal(row.Cell(9).GetString()),
                CreatedAt = ParseDt(row.Cell(10).GetString()),
            });
            added.Add(id); result.BatchItems++;
        }
    }

    private void AddWarrantyOptions(XLWorkbook wb, ImportResult result)
    {
        var ws = GetSheet(wb, "WarrantyOptions");
        if (ws == null) return;
        var existing = _db.BatchItemWarrantyOptions.Select(w => w.Id).ToHashSet();
        var added = new HashSet<Guid>();
        foreach (var row in DataRows(ws))
        {
            if (!Guid.TryParse(row.Cell(1).GetString(), out var id)) continue;
            if (!Guid.TryParse(row.Cell(2).GetString(), out var biId)) continue;
            if (existing.Contains(id) || added.Contains(id)) continue;
            _db.BatchItemWarrantyOptions.Add(new BatchItemWarrantyOption
            {
                Id = id,
                BatchItemId = biId,
                WarrantyMonths = ParseInt(row.Cell(3).GetString()),
                SellingPriceLKR = ParseDecimal(row.Cell(4).GetString()),
                IsDefault = bool.TryParse(row.Cell(5).GetString(), out var def) && def,
            });
            added.Add(id); result.WarrantyOptions++;
        }
    }

    private void AddSales(XLWorkbook wb, ImportResult result, Guid adminId)
    {
        var ws = GetSheet(wb, "Sales");
        if (ws == null) return;
        var existing = _db.Sales.Select(s => s.Id).ToHashSet();
        var added = new HashSet<Guid>();
        foreach (var row in DataRows(ws))
        {
            if (!Guid.TryParse(row.Cell(1).GetString(), out var id)) continue;
            if (existing.Contains(id) || added.Contains(id)) continue;
            _db.Sales.Add(new Sale
            {
                Id = id,
                InvoiceNumber = row.Cell(2).GetString().Trim(),
                SaleDate = ParseDt(row.Cell(3).GetString()),
                CustomerName = NullIfEmpty(row.Cell(4).GetString()),
                CustomerPhone = NullIfEmpty(row.Cell(5).GetString()),
                SubtotalLKR = ParseDecimal(row.Cell(6).GetString()),
                DiscountLKR = ParseDecimal(row.Cell(7).GetString()),
                TotalLKR = ParseDecimal(row.Cell(8).GetString()),
                CreatedAt = ParseDt(row.Cell(9).GetString()),
                CreatedBy = adminId,
            });
            added.Add(id); result.Sales++;
        }
    }

    private void AddSaleItems(XLWorkbook wb, ImportResult result)
    {
        var ws = GetSheet(wb, "SaleItems");
        if (ws == null) return;
        var existing = _db.SaleItems.Select(si => si.Id).ToHashSet();
        var added = new HashSet<Guid>();
        foreach (var row in DataRows(ws))
        {
            if (!Guid.TryParse(row.Cell(1).GetString(), out var id)) continue;
            if (!Guid.TryParse(row.Cell(2).GetString(), out var saleId)) continue;
            if (!Guid.TryParse(row.Cell(3).GetString(), out var biId)) continue;
            if (existing.Contains(id) || added.Contains(id)) continue;
            var wmCell = row.Cell(8);
            _db.SaleItems.Add(new SaleItem
            {
                Id = id,
                SaleId = saleId,
                BatchItemId = biId,
                Quantity = ParseInt(row.Cell(4).GetString()),
                UnitSellingPrice = ParseDecimal(row.Cell(5).GetString()),
                Discount = ParseDecimal(row.Cell(6).GetString()),
                LineTotal = ParseDecimal(row.Cell(7).GetString()),
                WarrantyMonths = wmCell.IsEmpty() ? null : ParseInt(wmCell.GetString()),
            });
            added.Add(id); result.SaleItems++;
        }
    }

    private void AddWarrantyClaims(XLWorkbook wb, ImportResult result, Guid adminId)
    {
        var ws = GetSheet(wb, "WarrantyClaims");
        if (ws == null) return;
        var existing = _db.WarrantyClaims.Select(c => c.Id).ToHashSet();
        var added = new HashSet<Guid>();
        foreach (var row in DataRows(ws))
        {
            if (!Guid.TryParse(row.Cell(1).GetString(), out var id)) continue;
            if (!Guid.TryParse(row.Cell(3).GetString(), out var siId)) continue;
            if (!Enum.TryParse<ClaimType>(row.Cell(4).GetString(), out var ct)) continue;
            if (!Enum.TryParse<ClaimStatus>(row.Cell(5).GetString(), out var cs)) continue;
            if (existing.Contains(id) || added.Contains(id)) continue;
            var resolvedCell = row.Cell(9);
            var repBiCell = row.Cell(11);
            var repCostCell = row.Cell(12);
            _db.WarrantyClaims.Add(new WarrantyClaim
            {
                Id = id,
                ClaimNumber = row.Cell(2).GetString().Trim(),
                SaleItemId = siId,
                ClaimType = ct,
                Status = cs,
                ComponentName = NullIfEmpty(row.Cell(6).GetString()),
                IssueDescription = row.Cell(7).GetString(),
                ClaimedAt = ParseDt(row.Cell(8).GetString()),
                ResolvedAt = resolvedCell.IsEmpty() ? null : ParseDt(resolvedCell.GetString()),
                ResolutionNotes = NullIfEmpty(row.Cell(10).GetString()),
                ReplacementBatchItemId = repBiCell.IsEmpty() ? null :
                    Guid.TryParse(repBiCell.GetString(), out var g) ? g : null,
                ReplacementCostLKR = repCostCell.IsEmpty() ? null : ParseDecimal(repCostCell.GetString()),
                StockDeducted = bool.TryParse(row.Cell(13).GetString(), out var sd) && sd,
                CreatedAt = ParseDt(row.Cell(14).GetString()),
                CreatedBy = adminId,
            });
            added.Add(id); result.WarrantyClaims++;
        }
    }

    private void AddStockAdjustments(XLWorkbook wb, ImportResult result, Guid adminId)
    {
        var ws = GetSheet(wb, "StockAdjustments");
        if (ws == null) return;
        var existing = _db.StockAdjustments.Select(a => a.Id).ToHashSet();
        var added = new HashSet<Guid>();
        foreach (var row in DataRows(ws))
        {
            if (!Guid.TryParse(row.Cell(1).GetString(), out var id)) continue;
            if (!Guid.TryParse(row.Cell(2).GetString(), out var biId)) continue;
            if (!Enum.TryParse<AdjustmentType>(row.Cell(3).GetString(), out var type)) continue;
            if (existing.Contains(id) || added.Contains(id)) continue;
            _db.StockAdjustments.Add(new StockAdjustment
            {
                Id = id,
                BatchItemId = biId,
                Type = type,
                QuantityChange = ParseInt(row.Cell(4).GetString()),
                Reason = row.Cell(5).GetString(),
                CreatedAt = ParseDt(row.Cell(6).GetString()),
                AdjustedBy = adminId,
            });
            added.Add(id); result.StockAdjustments++;
        }
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static void SetHeaders(IXLWorksheet ws, params string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
    }

    private static IEnumerable<IXLRow> DataRows(IXLWorksheet ws)
    {
        var last = ws.LastRowUsed();
        if (last == null || last.RowNumber() < 2) yield break;
        for (int r = 2; r <= last.RowNumber(); r++)
        {
            var row = ws.Row(r);
            if (!row.IsEmpty()) yield return row;
        }
    }

    private static IXLWorksheet? GetSheet(XLWorkbook wb, string name)
    {
        return wb.Worksheets.FirstOrDefault(w =>
            string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static DateTime ParseDt(string s) =>
        DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt : DateTime.UtcNow;

    private static decimal ParseDecimal(string s) =>
        decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;

    private static int ParseInt(string s) =>
        int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    public class ImportResult
    {
        public int Brands { get; set; }
        public int ProductTypes { get; set; }
        public int ConfigTypes { get; set; }
        public int Products { get; set; }
        public int ProductConfigs { get; set; }
        public int Batches { get; set; }
        public int BatchItems { get; set; }
        public int WarrantyOptions { get; set; }
        public int Sales { get; set; }
        public int SaleItems { get; set; }
        public int WarrantyClaims { get; set; }
        public int StockAdjustments { get; set; }
        public int Settings { get; set; }
    }
}
