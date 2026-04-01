using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Domain.Abstractions;

public interface IRefreshSessionRepository
{
    Task<RefreshSession> CreateAsync(RefreshSession session, CancellationToken cancellationToken = default);
    Task<RefreshSession?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(RefreshSession session, CancellationToken cancellationToken = default);
    Task<long> RevokeActiveSessionsByUserIdAsync(string userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
}
