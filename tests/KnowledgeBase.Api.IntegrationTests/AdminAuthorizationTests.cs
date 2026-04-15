using System.Net;
using System.Net.Http.Json;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Queries;
using KnowledgeBase.Domain.Enums;
using NSubstitute;
using Xunit;

namespace KnowledgeBase.Api.IntegrationTests;

public class AdminAuthorizationTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public AdminAuthorizationTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_WhenCallerIsAdmin_ReturnsOk()
    {
        _factory.AdminServiceSubstitute.GetAllUsersAsync(Arg.Any<GetUsersQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserListItemDto>());

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/users");
        request.Headers.Add("X-Test-Role", UserRole.Admin.ToString());

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdmin_WhenCallerIsAdmin_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/users")
        {
            Content = JsonContent.Create(new
            {
                firstName = "Alice",
                lastName = "Admin",
                username = "alice.admin",
                email = "alice.admin@example.com",
                password = "Password123!"
            })
        };
        request.Headers.Add("X-Test-Role", UserRole.Admin.ToString());

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdmin_WhenCallerIsMasterAdmin_ReturnsCreated()
    {
        _factory.AdminServiceSubstitute.CreateAdminAsync(Arg.Any<CreateAdminUserDto>(), Arg.Any<CancellationToken>())
            .Returns(new UserDto
            {
                Id = "admin-1",
                FirstName = "Alice",
                LastName = "Admin",
                Username = "alice.admin",
                Email = "alice.admin@example.com",
                IsActive = true,
                Role = UserRole.Admin
            });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/users")
        {
            Content = JsonContent.Create(new
            {
                firstName = "Alice",
                lastName = "Admin",
                username = "alice.admin",
                email = "alice.admin@example.com",
                password = "Password123!"
            })
        };
        request.Headers.Add("X-Test-Role", UserRole.MasterAdmin.ToString());

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
