using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Infrastructure.Persistence;
using KnowledgeBase.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBase.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(
            configuration.GetSection(MongoDbSettings.SectionName));

        services.AddSingleton<MongoContext>();
        services.AddScoped<INoteRepository, NoteRepository>();

        return services;
    }
}