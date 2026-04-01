using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Infrastructure.Persistence;
using KnowledgeBase.Infrastructure.Queries;
using KnowledgeBase.Infrastructure.Repositories;
using KnowledgeBase.Infrastructure.Security;
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
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));
        services.Configure<RefreshTokenSettings>(
            configuration.GetSection(RefreshTokenSettings.SectionName));

        services.AddSingleton<MongoContext>();
        services.AddScoped<IAdminUserReader, AdminUserReader>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<IRefreshSessionRepository, RefreshSessionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IRefreshTokenProvider, RefreshTokenProvider>();

        return services;
    }
}
