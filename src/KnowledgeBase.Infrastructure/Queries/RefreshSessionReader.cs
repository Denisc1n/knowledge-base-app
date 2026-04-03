using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using MongoDB.Driver;

namespace KnowledgeBase.Infrastructure.Queries;

public class RefreshSessionReader : IRefreshSessionReader
{
    private readonly MongoContext _context;

    public RefreshSessionReader(MongoContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SessionDto>> GetByUserIdAsync(
        string userId,
        string? currentRefreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.RefreshSessions
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.LastSeenAtUtc)
            .Project(x => new SessionDto
            {
                Id = x.Id,
                UserAgent = x.UserAgent,
                IpAddress = x.LastSeenIp ?? x.CreatedByIp,
                CreatedAtUtc = x.CreatedAtUtc,
                LastSeenAtUtc = x.LastSeenAtUtc,
                ExpiresAtUtc = x.ExpiresAtUtc,
                RevokedAtUtc = x.RevokedAtUtc,
                RevokedReason = x.RevokedReason,
                IsCurrent = !string.IsNullOrWhiteSpace(currentRefreshTokenHash) && x.TokenHash == currentRefreshTokenHash,
                IsActive = !x.RevokedAtUtc.HasValue && x.ExpiresAtUtc > now
            })
            .ToListAsync(cancellationToken);
    }
}
