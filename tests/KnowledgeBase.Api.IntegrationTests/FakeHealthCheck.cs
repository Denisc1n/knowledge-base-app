using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KnowledgeBase.Api.IntegrationTests;

public class FakeHealthCheck : IHealthCheck
{
    private readonly string _description;

    public FakeHealthCheck(string description)
    {
        _description = description;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy(_description));
    }
}
