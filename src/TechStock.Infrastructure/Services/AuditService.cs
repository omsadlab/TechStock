using TechStock.Application.Interfaces;
using TechStock.Domain.Entities;
using TechStock.Infrastructure.Data;

namespace TechStock.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(Guid userId, string action, string entityType, Guid entityId,
        string? oldValue = null, string? newValue = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
        });
        await _db.SaveChangesAsync();
    }
}
