namespace TechStock.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(Guid userId, string action, string entityType, Guid entityId,
        string? oldValue = null, string? newValue = null);
}
