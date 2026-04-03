using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Infrastructure.Persistence;
using MongoDB.Driver;

namespace KnowledgeBase.Infrastructure.Queries;

public class AuthAuditReader : IAuthAuditReader
{
    private readonly MongoContext _context;

    public AuthAuditReader(MongoContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AuthAuditEventDto>> GetRecentByUserIdAsync(
        string userId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 100);

        return await _context.AuthAuditEvents
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.OccurredAtUtc)
            .Project(x => new AuthAuditEventDto
            {
                EventType = x.EventType,
                Detail = x.Detail,
                UserAgent = x.UserAgent,
                IpAddress = x.IpAddress,
                OccurredAtUtc = x.OccurredAtUtc
            })
            .Limit(normalizedLimit)
            .ToListAsync(cancellationToken);
    }
}
