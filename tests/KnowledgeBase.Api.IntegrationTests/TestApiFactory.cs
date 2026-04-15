using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace KnowledgeBase.Api.IntegrationTests;

public class TestApiFactory : WebApplicationFactory<Program>
{
    public IAuthService AuthServiceSubstitute { get; } = Substitute.For<IAuthService>();
    public INoteService NoteServiceSubstitute { get; } = Substitute.For<INoteService>();
    public IAdminService AdminServiceSubstitute { get; } = Substitute.For<IAdminService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "KnowledgeBase.Api.Tests",
                ["Jwt:Audience"] = "KnowledgeBase.Api.Tests.Client",
                ["Jwt:SecretKey"] = "integration-tests-secret-key-1234567890",
                ["Jwt:ExpirationMinutes"] = "60",
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "knowledgebase-tests",
                ["BootstrapAdmin:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
                options.Registrations.Add(new HealthCheckRegistration(
                    "self",
                    _ => new FakeHealthCheck("API is running."),
                    HealthStatus.Unhealthy,
                    tags: null));
                options.Registrations.Add(new HealthCheckRegistration(
                    "mongo",
                    _ => new FakeHealthCheck("MongoDB is reachable."),
                    HealthStatus.Unhealthy,
                    tags: null));
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName,
                _ => { });

            services.PostConfigureAll<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            });

            services.RemoveAll<IAuthService>();
            services.RemoveAll<INoteService>();
            services.RemoveAll<IAdminService>();

            services.AddSingleton(AuthServiceSubstitute);
            services.AddSingleton(NoteServiceSubstitute);
            services.AddSingleton(AdminServiceSubstitute);
        });
    }
}
