using KnowledgeBase.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace KnowledgeBase.Api.HealthChecks;

public class MongoHealthCheck : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private readonly IMongoClient _mongoClient;
    private readonly MongoDbSettings _settings;

    public MongoHealthCheck(IMongoClient mongoClient, IOptions<MongoDbSettings> options)
    {
        _mongoClient = mongoClient;
        _settings = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Timeout);

            var database = _mongoClient.GetDatabase(_settings.DatabaseName);

            await database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: timeoutCts.Token);

            return HealthCheckResult.Healthy("MongoDB is reachable.");
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("MongoDB health check timed out.", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB is unavailable.", ex);
        }
    }
}
