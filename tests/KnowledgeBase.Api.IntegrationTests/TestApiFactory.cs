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
    private const string TestJwtIssuer = "KnowledgeBase.Api.Tests";
    private const string TestJwtAudience = "KnowledgeBase.Api.Tests.Client";
    private const string TestJwtSecretKey = "integration-tests-secret-key-1234567890";
    private const string TestMongoConnectionString = "mongodb://localhost:27017";
    private const string TestMongoDatabaseName = "knowledgebase-tests";

    public IAuthService AuthServiceSubstitute { get; } = Substitute.For<IAuthService>();
    public INoteService NoteServiceSubstitute { get; } = Substitute.For<INoteService>();
    public IAdminService AdminServiceSubstitute { get; } = Substitute.For<IAdminService>();

    public TestApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtAudience);
        Environment.SetEnvironmentVariable("Jwt__SecretKey", TestJwtSecretKey);
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("MongoDb__ConnectionString", TestMongoConnectionString);
        Environment.SetEnvironmentVariable("MongoDb__DatabaseName", TestMongoDatabaseName);
        Environment.SetEnvironmentVariable("BootstrapAdmin__Enabled", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:SecretKey"] = TestJwtSecretKey,
                ["Jwt:ExpirationMinutes"] = "60",
                ["MongoDb:ConnectionString"] = TestMongoConnectionString,
                ["MongoDb:DatabaseName"] = TestMongoDatabaseName,
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
