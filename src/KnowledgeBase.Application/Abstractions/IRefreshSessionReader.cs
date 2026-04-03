using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.Abstractions;

public interface IRefreshSessionReader
{
    Task<IReadOnlyList<SessionDto>> GetByUserIdAsync(
        string userId,
        string? currentRefreshTokenHash,
        CancellationToken cancellationToken = default);
}
