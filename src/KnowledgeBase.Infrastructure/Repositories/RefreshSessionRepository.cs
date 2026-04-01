using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using MongoDB.Driver;

namespace KnowledgeBase.Infrastructure.Repositories;

public class RefreshSessionRepository : IRefreshSessionRepository
{
    private readonly MongoContext _context;

    public RefreshSessionRepository(MongoContext context)
    {
        _context = context;
    }

    public async Task<RefreshSession> CreateAsync(RefreshSession session, CancellationToken cancellationToken = default)
    {
        await _context.RefreshSessions.InsertOneAsync(session, cancellationToken: cancellationToken);
        return session;
    }

    public async Task<RefreshSession?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshSessions
            .Find(x => x.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(RefreshSession session, CancellationToken cancellationToken = default)
    {
        var result = await _context.RefreshSessions.ReplaceOneAsync(
            x => x.Id == session.Id,
            session,
            cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }

    public async Task<long> RevokeActiveSessionsByUserIdAsync(
        string userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var update = Builders<RefreshSession>.Update.Set(x => x.RevokedAtUtc, revokedAtUtc);

        var result = await _context.RefreshSessions.UpdateManyAsync(
            x => x.UserId == userId && !x.RevokedAtUtc.HasValue,
            update,
            cancellationToken: cancellationToken);

        return result.ModifiedCount;
    }
}
