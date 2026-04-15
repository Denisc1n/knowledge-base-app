using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Enums;
using NSubstitute;
using Xunit;

namespace KnowledgeBase.Api.IntegrationTests;

public class VersioningAndHealthTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public VersioningAndHealthTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_ReturnsHealthy_WithCorrelationAndVersionHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-Id", "corr-123");

        using var response = await _client.SendAsync(request);
        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("corr-123", response.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Equal("1.0", response.Headers.GetValues("api-supported-versions").Single());
        Assert.Equal("Healthy", payload?["status"]?.GetValue<string>());
    }

    [Fact]
    public async Task HealthReady_ReturnsHealthy()
    {
        using var response = await _client.GetAsync("/health/ready");
        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", payload?["status"]?.GetValue<string>());
        Assert.Equal("Healthy", payload?["entries"]?["mongo"]?["status"]?.GetValue<string>());
    }

    [Fact]
    public async Task VersionedAuthLoginRoute_ReturnsOk()
    {
        _factory.AuthServiceSubstitute.LoginAsync(
                Arg.Any<LoginDto>(),
                Arg.Any<SessionContextDto>(),
                Arg.Any<CancellationToken>())
            .Returns(new LoginResultDto
            {
                AccessToken = "jwt-token",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                RefreshToken = "refresh-token",
                RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                User = new UserDto
                {
                    Id = "user-1",
                    FirstName = "Test",
                    LastName = "User",
                    Username = "test.user",
                    Email = "test@example.com",
                    IsActive = true,
                    Role = UserRole.User
                }
            });

        using var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "test.user",
            password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("1.0", response.Headers.GetValues("api-supported-versions").Single());
    }

    [Fact]
    public async Task VersionedNotesRoute_ReturnsOk()
    {
        _factory.NoteServiceSubstitute.GetAllAsync("test-user-id", Arg.Any<CancellationToken>())
            .Returns(new List<NoteDto>
            {
                new()
                {
                    Id = "note-1",
                    Title = "Versioned note",
                    Content = "content",
                    Tags = ["v1"],
                    Category = "test",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                }
            });

        using var response = await _client.GetAsync("/api/v1/notes");
        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.AsArray().Count > 0);
    }
}
