using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Domain.Abstractions;

public interface IAuthAuditRepository
{
    Task<AuthAuditEvent> CreateAsync(AuthAuditEvent auditEvent, CancellationToken cancellationToken = default);
}
