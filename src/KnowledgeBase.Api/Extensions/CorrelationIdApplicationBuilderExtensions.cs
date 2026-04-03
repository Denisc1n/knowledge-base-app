using KnowledgeBase.Api.Observability;

namespace KnowledgeBase.Api.Extensions;

public static class CorrelationIdApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
