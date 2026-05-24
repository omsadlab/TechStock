namespace TechStock.Application.Interfaces;

public interface IExcelExportService
{
    Task<byte[]> ExportProductsAsync(bool includeCost);
    Task<byte[]> ExportInventoryAsync();
    Task<byte[]> ExportBatchAsync(Guid batchId);
    Task<byte[]> ExportSalesAsync(DateTime from, DateTime to);
}

public interface IExcelImportService
{
    Task<ImportResult> ImportProductsAsync(Stream fileStream, string onDuplicate);
    Task<ImportResult> ImportBatchItemsAsync(Guid batchId, Stream fileStream);
    byte[] GetProductsTemplate();
    byte[] GetBatchItemsTemplate();
}

public class ImportResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = [];
}
