using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;

namespace KnowledgeBase.Infrastructure.Repositories;

public class AuthAuditRepository : IAuthAuditRepository
{
    private readonly MongoContext _context;

    public AuthAuditRepository(MongoContext context)
    {
        _context = context;
    }

    public async Task<AuthAuditEvent> CreateAsync(
        AuthAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        await _context.AuthAuditEvents.InsertOneAsync(auditEvent, cancellationToken: cancellationToken);
        return auditEvent;
    }
}
