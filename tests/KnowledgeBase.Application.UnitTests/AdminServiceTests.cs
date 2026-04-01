using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.Services;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Queries;
using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;
using NSubstitute;

namespace KnowledgeBase.Application.UnitTests;

public class AdminServiceTests
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminUserReader _adminUserReader;
    private readonly INoteRepository _noteRepository;
    private readonly IRefreshSessionRepository _refreshSessionRepository;
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _adminUserReader = Substitute.For<IAdminUserReader>();
        _noteRepository = Substitute.For<INoteRepository>();
        _refreshSessionRepository = Substitute.For<IRefreshSessionRepository>();
        _service = new AdminService(_userRepository, _adminUserReader, _noteRepository, _refreshSessionRepository);
    }

    [Fact]
    public async Task GetAllUsersAsync_NormalizesPagination_AndReturnsRepositoryProjection()
    {
        var projectedUsers = new List<UserListItemDto>
        {
            new()
            {
                Name = "Ada",
                LastName = "Lovelace",
                Email = "ada@example.com",
                Status = true,
                RegisteredAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        _adminUserReader.GetAllAsync(1, 1, Arg.Any<CancellationToken>()).Returns(projectedUsers);

        var result = await _service.GetAllUsersAsync(new GetUsersQuery
        {
            Page = 0,
            PageSize = -5
        });

        Assert.Same(projectedUsers, result);
        await _adminUserReader.Received(1).GetAllAsync(1, 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetUserActiveStatusAsync_WhenUserExists_UpdatesStatus_AndRevokesSessions()
    {
        var user = new User
        {
            Id = "user-1",
            FirstName = "Ada",
            LastName = "Lovelace",
            Username = "ada",
            Email = "ada@example.com",
            PasswordHash = "hash",
            IsActive = true,
            IsAdmin = false
        };

        _userRepository.GetByIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(user);
        _userRepository.UpdateAsync(user, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.SetUserActiveStatusAsync("user-1", false);

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(x => x.Id == "user-1" && !x.IsActive),
            Arg.Any<CancellationToken>());
        await _refreshSessionRepository.Received(1).RevokeActiveSessionsByUserIdAsync(
            "user-1",
            Arg.Is<DateTime>(x => x > DateTime.UtcNow.AddMinutes(-1)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetUserActiveStatusAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        _userRepository.GetByIdAsync("missing", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _service.SetUserActiveStatusAsync("missing", false);

        Assert.Null(result);
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _refreshSessionRepository.DidNotReceive().RevokeActiveSessionsByUserIdAsync(
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetUserActiveStatusAsync_WhenReEnablingUser_DoesNotRevokeSessions()
    {
        var user = new User
        {
            Id = "user-2",
            FirstName = "Grace",
            LastName = "Hopper",
            Username = "grace",
            Email = "grace@example.com",
            PasswordHash = "hash",
            IsActive = false,
            IsAdmin = true
        };

        _userRepository.GetByIdAsync("user-2", Arg.Any<CancellationToken>()).Returns(user);
        _userRepository.UpdateAsync(user, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.SetUserActiveStatusAsync("user-2", true);

        Assert.NotNull(result);
        Assert.True(result!.IsActive);
        await _refreshSessionRepository.DidNotReceive().RevokeActiveSessionsByUserIdAsync(
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteUserNoteAsync_ForwardsScopedDeleteToRepository()
    {
        _noteRepository.DeleteAsync("note-9", "user-9", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.DeleteUserNoteAsync("user-9", "note-9");

        Assert.True(result);
        await _noteRepository.Received(1).DeleteAsync("note-9", "user-9", Arg.Any<CancellationToken>());
    }
}
