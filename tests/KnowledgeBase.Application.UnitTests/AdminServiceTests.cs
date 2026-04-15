using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Exceptions;
using KnowledgeBase.Application.Queries;
using KnowledgeBase.Application.Security;
using KnowledgeBase.Application.Services;
using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;
using NSubstitute;

namespace KnowledgeBase.Application.UnitTests;

public class AdminServiceTests
{
    private readonly IUserRepository _userRepository;
    private readonly IUserReader _userReader;
    private readonly INoteRepository _noteRepository;
    private readonly IRefreshSessionRepository _refreshSessionRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AdminService _service;

    public AdminServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _userReader = Substitute.For<IUserReader>();
        _noteRepository = Substitute.For<INoteRepository>();
        _refreshSessionRepository = Substitute.For<IRefreshSessionRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _service = new AdminService(_userRepository, _userReader, _noteRepository, _refreshSessionRepository, _passwordHasher);
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
                RegisteredAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Role = UserRole.User.ToString()
            }
        };

        _userReader.GetAllAsync(
                Arg.Is<GetUsersQuery>(x =>
                    x.Page == 1 &&
                    x.PageSize == 1 &&
                    x.IsActive == true &&
                    x.IsAdmin == false &&
                    x.CreatedDate == new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) &&
                    x.SortBy == UserSortBy.LastName &&
                    x.SortDirection == SortDirection.Asc),
                Arg.Any<CancellationToken>())
            .Returns(projectedUsers);

        var result = await _service.GetAllUsersAsync(new GetUsersQuery
        {
            Page = 0,
            PageSize = -5,
            IsActive = true,
            IsAdmin = false,
            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SortBy = UserSortBy.LastName,
            SortDirection = SortDirection.Asc
        });

        Assert.Same(projectedUsers, result);
        await _userReader.Received(1).GetAllAsync(
            Arg.Is<GetUsersQuery>(x =>
                x.Page == 1 &&
                x.PageSize == 1 &&
                x.IsActive == true &&
                x.IsAdmin == false &&
                x.CreatedDate == new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) &&
                x.SortBy == UserSortBy.LastName &&
                x.SortDirection == SortDirection.Asc),
            Arg.Any<CancellationToken>());
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
            Role = UserRole.User
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
            RefreshSessionRevocationReasons.UserDeactivated,
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
            Role = UserRole.Admin
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
    public async Task CreateAdminAsync_WhenRequestIsValid_CreatesAdminUser()
    {
        _userRepository.UsernameExistsAsync("alice.admin", Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.EmailExistsAsync("alice.admin@example.com", Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash("Password123!").Returns("hashed-password");
        _userRepository.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var user = callInfo.ArgAt<User>(0);
                user.Id = "admin-1";
                return user;
            });

        var result = await _service.CreateAdminAsync(new CreateAdminUserDto
        {
            FirstName = "Alice",
            LastName = "Admin",
            Username = "Alice.Admin",
            Email = "Alice.Admin@example.com",
            Password = "Password123!"
        });

        Assert.Equal("admin-1", result.Id);
        Assert.Equal(UserRole.Admin, result.Role);
        await _userRepository.Received(1).CreateAsync(
            Arg.Is<User>(x => x.Role == UserRole.Admin && x.IsActive),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PromoteToAdminAsync_WhenUserExists_UpdatesRole_AndRevokesSessions()
    {
        var user = new User
        {
            Id = "user-3",
            FirstName = "Linus",
            LastName = "Torvalds",
            Username = "linus",
            Email = "linus@example.com",
            PasswordHash = "hash",
            SecurityStamp = "old-stamp",
            IsActive = true,
            Role = UserRole.User
        };

        _userRepository.GetByIdAsync("user-3", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _service.PromoteToAdminAsync("user-3");

        Assert.NotNull(result);
        Assert.Equal(UserRole.Admin, result!.Role);
        Assert.NotEqual("old-stamp", user.SecurityStamp);
        await _refreshSessionRepository.Received(1).RevokeActiveSessionsByUserIdAsync(
            "user-3",
            Arg.Any<DateTime>(),
            RefreshSessionRevocationReasons.RoleChanged,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DemoteAdminAsync_WhenTargetIsMasterAdmin_ThrowsInvalidAdminOperationException()
    {
        _userRepository.GetByIdAsync("master-1", Arg.Any<CancellationToken>())
            .Returns(new User
            {
                Id = "master-1",
                FirstName = "Master",
                LastName = "Admin",
                Username = "master",
                Email = "master@example.com",
                PasswordHash = "hash",
                IsActive = true,
                Role = UserRole.MasterAdmin
            });

        var ex = await Assert.ThrowsAsync<InvalidAdminOperationException>(() =>
            _service.DemoteAdminAsync("master-1"));

        Assert.Equal("The master admin cannot be demoted.", ex.Message);
    }

    [Fact]
    public async Task SetUserActiveStatusAsync_WhenTargetIsMasterAdminAndDeactivationRequested_ThrowsInvalidAdminOperationException()
    {
        _userRepository.GetByIdAsync("master-1", Arg.Any<CancellationToken>())
            .Returns(new User
            {
                Id = "master-1",
                FirstName = "Master",
                LastName = "Admin",
                Username = "master",
                Email = "master@example.com",
                PasswordHash = "hash",
                IsActive = true,
                Role = UserRole.MasterAdmin
            });

        var ex = await Assert.ThrowsAsync<InvalidAdminOperationException>(() =>
            _service.SetUserActiveStatusAsync("master-1", false));

        Assert.Equal("The master admin account cannot be deactivated.", ex.Message);
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
