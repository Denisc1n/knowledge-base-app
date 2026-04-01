using FluentValidation;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.Services;
using KnowledgeBase.Application.Validators;

namespace KnowledgeBase.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<INoteService, NoteService>();

        services.AddValidatorsFromAssemblyContaining<CreateNoteDtoValidator>();

        return services;
    }
}
