using KnowledgeBase.Infrastructure.Persistence;
using MongoDB.Bson;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KnowledgeBase.Api.HealthChecks;

public class MongoHealthCheck : IHealthCheck
{
    private readonly MongoContext _context;

    public MongoHealthCheck(MongoContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Users.Database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("MongoDB is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB is unavailable.", ex);
        }
    }
}
