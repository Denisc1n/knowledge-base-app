using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KnowledgeBase.Api.HealthChecks;

public static class HealthCheckResponseWriter
{
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                x => x.Key,
                x => new
                {
                    status = x.Value.Status.ToString(),
                    description = x.Value.Description,
                    duration = x.Value.Duration.TotalMilliseconds,
                    error = x.Value.Exception?.Message
                })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
