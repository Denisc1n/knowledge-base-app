using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.Abstractions;

public interface IAuthAuditReader
{
    Task<IReadOnlyList<AuthAuditEventDto>> GetRecentByUserIdAsync(
        string userId,
        int limit,
        CancellationToken cancellationToken = default);
}
