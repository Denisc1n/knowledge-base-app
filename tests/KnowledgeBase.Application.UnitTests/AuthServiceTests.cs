using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Exceptions;
using KnowledgeBase.Application.Services;
using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;
using NSubstitute;

namespace KnowledgeBase.Application.UnitTests;

public class AuthServiceTests
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshSessionRepository _refreshSessionRepository;
    private readonly IRefreshSessionReader _refreshSessionReader;
    private readonly IAuthAuditRepository _authAuditRepository;
    private readonly IAuthAuditReader _authAuditReader;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenProvider _refreshTokenProvider;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _refreshSessionRepository = Substitute.For<IRefreshSessionRepository>();
        _refreshSessionReader = Substitute.For<IRefreshSessionReader>();
        _authAuditRepository = Substitute.For<IAuthAuditRepository>();
        _authAuditReader = Substitute.For<IAuthAuditReader>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _refreshTokenProvider = Substitute.For<IRefreshTokenProvider>();
        _service = new AuthService(
            _userRepository,
            _refreshSessionReader,
            _authAuditRepository,
            _authAuditReader,
            _passwordHasher,
            _jwtTokenGenerator,
            _refreshSessionRepository,
            _refreshTokenProvider);
    }

    [Fact]
    public async Task SignupAsync_WhenUserIsValid_CreatesNormalizedUser()
    {
        var dto = new SignupUserDto
        {
            FirstName = "  Jane ",
            LastName = " Doe  ",
            Username = "  Jane.Doe ",
            Email = "  JANE@EXAMPLE.COM ",
            Password = "Password123!"
        };

        _userRepository.UsernameExistsAsync("jane.doe", Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.EmailExistsAsync("jane@example.com", Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash("Password123!").Returns("hashed-password");
        _userRepository.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var user = callInfo.ArgAt<User>(0);
                user.Id = "user-1";
                return user;
            });

        var result = await _service.SignupAsync(dto);

        await _userRepository.Received(1).CreateAsync(
            Arg.Is<User>(u =>
                u.FirstName == "Jane" &&
                u.LastName == "Doe" &&
                u.Username == "jane.doe" &&
                u.Email == "jane@example.com" &&
                u.PasswordHash == "hashed-password" &&
                !string.IsNullOrWhiteSpace(u.SecurityStamp) &&
                u.IsActive &&
                !u.IsAdmin),
            Arg.Any<CancellationToken>());

        Assert.Equal("user-1", result.Id);
        Assert.Equal("jane.doe", result.Username);
        Assert.Equal("jane@example.com", result.Email);
        Assert.True(result.IsActive);
        Assert.False(result.IsAdmin);
    }

    [Fact]
    public async Task SignupAsync_WhenUsernameAlreadyExists_ThrowsDuplicateUserException()
    {
        _userRepository.UsernameExistsAsync("existing", Arg.Any<CancellationToken>()).Returns(true);

        var ex = await Assert.ThrowsAsync<DuplicateUserException>(() =>
            _service.SignupAsync(new SignupUserDto
            {
                FirstName = "Jane",
                LastName = "Doe",
                Username = "existing",
                Email = "jane@example.com",
                Password = "Password123!"
            }));

        Assert.Equal("username", ex.FieldName);
        await _userRepository.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsTokenAndUser()
    {
        var user = new User
        {
            Id = "user-2",
            FirstName = "Jane",
            LastName = "Doe",
            Username = "jane",
            Email = "jane@example.com",
            PasswordHash = "hash",
            SecurityStamp = "stamp-1",
            IsActive = true,
            IsAdmin = true
        };

        _userRepository.GetByUsernameAsync("jane", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Password123!", "hash").Returns(true);
        _jwtTokenGenerator.Generate(user).Returns(new TokenResult
        {
            AccessToken = "jwt-token",
            ExpiresAtUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        _refreshTokenProvider.Generate().Returns(new RefreshTokenResult
        {
            Token = "refresh-token",
            ExpiresAtUtc = new DateTime(2030, 1, 8, 0, 0, 0, DateTimeKind.Utc)
        });
        _refreshTokenProvider.Hash("refresh-token").Returns("refresh-token-hash");

        var result = await _service.LoginAsync(new LoginDto
        {
            Username = "  Jane ",
            Password = "Password123!"
        }, new SessionContextDto
        {
            IpAddress = "127.0.0.1",
            UserAgent = "UnitTestBrowser"
        });

        Assert.Equal("jwt-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal("user-2", result.User.Id);
        Assert.True(result.User.IsAdmin);
        await _refreshSessionRepository.Received(1).CreateAsync(
            Arg.Is<RefreshSession>(s =>
                s.UserId == "user-2" &&
                s.TokenHash == "refresh-token-hash" &&
                s.CreatedByIp == "127.0.0.1" &&
                s.LastSeenIp == "127.0.0.1" &&
                s.UserAgent == "UnitTestBrowser"),
            Arg.Any<CancellationToken>());
        await _authAuditRepository.Received(1).CreateAsync(
            Arg.Is<AuthAuditEvent>(x =>
                x.UserId == "user-2" &&
                x.EventType == "login"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsInactive_ThrowsAuthenticationException()
    {
        var user = new User
        {
            Id = "user-3",
            FirstName = "John",
            LastName = "Doe",
            Username = "john",
            Email = "john@example.com",
            PasswordHash = "hash",
            SecurityStamp = "stamp-2",
            IsActive = false
        };

        _userRepository.GetByUsernameAsync("john", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Password123!", "hash").Returns(true);

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() =>
            _service.LoginAsync(new LoginDto
            {
                Username = "john",
                Password = "Password123!"
            }, new SessionContextDto()));

        Assert.Equal("This user is inactive.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_ThrowsAuthenticationException()
    {
        var user = new User
        {
            Id = "user-4",
            FirstName = "John",
            LastName = "Doe",
            Username = "john",
            Email = "john@example.com",
            PasswordHash = "hash",
            SecurityStamp = "stamp-3",
            IsActive = true
        };

        _userRepository.GetByUsernameAsync("john", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong", "hash").Returns(false);

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() =>
            _service.LoginAsync(new LoginDto
            {
                Username = "john",
                Password = "wrong"
            }, new SessionContextDto()));

        Assert.Equal("Invalid username or password.", ex.Message);
    }

    [Fact]
    public async Task RefreshAsync_WhenSessionIsValid_RotatesRefreshToken()
    {
        var session = new RefreshSession
        {
            Id = "session-1",
            UserId = "user-5",
            TokenHash = "old-hash",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(6)
        };
        var user = new User
        {
            Id = "user-5",
            FirstName = "Jane",
            LastName = "Doe",
            Username = "jane",
            Email = "jane@example.com",
            PasswordHash = "hash",
            SecurityStamp = "stamp-4",
            IsActive = true
        };

        _refreshTokenProvider.Hash("old-refresh-token").Returns("old-hash");
        _refreshSessionRepository.GetByTokenHashAsync("old-hash", Arg.Any<CancellationToken>()).Returns(session);
        _userRepository.GetByIdAsync("user-5", Arg.Any<CancellationToken>()).Returns(user);
        _refreshTokenProvider.Generate().Returns(new RefreshTokenResult
        {
            Token = "new-refresh-token",
            ExpiresAtUtc = new DateTime(2030, 1, 10, 0, 0, 0, DateTimeKind.Utc)
        });
        _refreshTokenProvider.Hash("new-refresh-token").Returns("new-hash");
        _jwtTokenGenerator.Generate(user).Returns(new TokenResult
        {
            AccessToken = "new-access-token",
            ExpiresAtUtc = new DateTime(2030, 1, 3, 0, 0, 0, DateTimeKind.Utc)
        });

        var result = await _service.RefreshAsync(new RefreshTokenDto
        {
            RefreshToken = "old-refresh-token"
        }, new SessionContextDto
        {
            IpAddress = "192.168.0.50",
            UserAgent = "UnitTestBrowser/2.0"
        });

        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);
        Assert.Equal("new-hash", session.ReplacedByTokenHash);
        Assert.NotNull(session.RevokedAtUtc);
        Assert.Equal("rotated", session.RevokedReason);
        Assert.Equal("192.168.0.50", session.LastSeenIp);
        await _refreshSessionRepository.Received(1).UpdateAsync(session, Arg.Any<CancellationToken>());
        await _refreshSessionRepository.Received(1).CreateAsync(
            Arg.Is<RefreshSession>(s =>
                s.UserId == "user-5" &&
                s.TokenHash == "new-hash" &&
                s.CreatedByIp == "192.168.0.50" &&
                s.UserAgent == "UnitTestBrowser/2.0"),
            Arg.Any<CancellationToken>());
        await _authAuditRepository.Received(1).CreateAsync(
            Arg.Is<AuthAuditEvent>(x =>
                x.UserId == "user-5" &&
                x.EventType == "refresh"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_WhenSessionIsExpired_ThrowsAuthenticationException()
    {
        _refreshTokenProvider.Hash("expired").Returns("expired-hash");
        _refreshSessionRepository.GetByTokenHashAsync("expired-hash", Arg.Any<CancellationToken>())
            .Returns(new RefreshSession
            {
                Id = "session-2",
                UserId = "user-6",
                TokenHash = "expired-hash",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1)
            });

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() =>
            _service.RefreshAsync(new RefreshTokenDto { RefreshToken = "expired" }, new SessionContextDto()));

        Assert.Equal("Invalid or expired refresh token.", ex.Message);
    }

    [Fact]
    public async Task LogoutAsync_WhenSessionExists_RevokesIt()
    {
        var session = new RefreshSession
        {
            Id = "session-3",
            UserId = "user-7",
            TokenHash = "logout-hash",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(6)
        };

        _refreshTokenProvider.Hash("logout-token").Returns("logout-hash");
        _refreshSessionRepository.GetByTokenHashAsync("logout-hash", Arg.Any<CancellationToken>()).Returns(session);

        await _service.LogoutAsync(new RefreshTokenDto
        {
            RefreshToken = "logout-token"
        });

        Assert.NotNull(session.RevokedAtUtc);
        Assert.Equal("logout", session.RevokedReason);
        await _refreshSessionRepository.Received(1).UpdateAsync(session, Arg.Any<CancellationToken>());
        await _authAuditRepository.Received(1).CreateAsync(
            Arg.Is<AuthAuditEvent>(x =>
                x.UserId == "user-7" &&
                x.RefreshSessionId == "session-3" &&
                x.EventType == "logout"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenCurrentPasswordIsValid_UpdatesHash_RotatesSecurityStamp_AndRevokesSessions()
    {
        var user = new User
        {
            Id = "user-8",
            FirstName = "Jane",
            LastName = "Doe",
            Username = "jane",
            Email = "jane@example.com",
            PasswordHash = "old-hash",
            SecurityStamp = "old-stamp",
            IsActive = true
        };

        _userRepository.GetByIdAsync("user-8", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("CurrentPassword123!", "old-hash").Returns(true);
        _passwordHasher.Hash("NewPassword123!").Returns("new-hash");

        await _service.ResetPasswordAsync("user-8", new ResetPasswordDto
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NewPassword123!"
        });

        Assert.Equal("new-hash", user.PasswordHash);
        Assert.NotEqual("old-stamp", user.SecurityStamp);
        Assert.False(string.IsNullOrWhiteSpace(user.SecurityStamp));
        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _refreshSessionRepository.Received(1).RevokeActiveSessionsByUserIdAsync(
            "user-8",
            Arg.Any<DateTime>(),
            "password_reset",
            Arg.Any<CancellationToken>());
        await _authAuditRepository.Received(1).CreateAsync(
            Arg.Is<AuthAuditEvent>(x =>
                x.UserId == "user-8" &&
                x.EventType == "reset_password"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenCurrentPasswordIsInvalid_ThrowsAuthenticationException()
    {
        var user = new User
        {
            Id = "user-9",
            FirstName = "Jane",
            LastName = "Doe",
            Username = "jane",
            Email = "jane@example.com",
            PasswordHash = "old-hash",
            SecurityStamp = "stamp-9",
            IsActive = true
        };

        _userRepository.GetByIdAsync("user-9", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong-current-password", "old-hash").Returns(false);

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() =>
            _service.ResetPasswordAsync("user-9", new ResetPasswordDto
            {
                CurrentPassword = "wrong-current-password",
                NewPassword = "NewPassword123!"
            }));

        Assert.Equal("Current password is invalid.", ex.Message);
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _refreshSessionRepository.DidNotReceive().RevokeActiveSessionsByUserIdAsync(
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSessionsAsync_WhenCurrentRefreshTokenIsProvided_MarksCurrentSession()
    {
        var sessions = new List<SessionDto>
        {
            new() { Id = "session-1", IsCurrent = true, IsActive = true },
            new() { Id = "session-2", IsCurrent = false, IsActive = true }
        };

        _refreshTokenProvider.Hash("current-refresh-token").Returns("current-refresh-token-hash");
        _refreshSessionReader.GetByUserIdAsync("user-10", "current-refresh-token-hash", Arg.Any<CancellationToken>())
            .Returns(sessions);

        var result = await _service.GetSessionsAsync("user-10", "current-refresh-token");

        Assert.Equal(2, result.Count);
        Assert.Single(result, x => x.IsCurrent);
    }

    [Fact]
    public async Task LogoutAllAsync_WhenUserIsValid_RotatesSecurityStamp_RevokesSessions_AndWritesAuditEvent()
    {
        var user = new User
        {
            Id = "user-11",
            FirstName = "Jane",
            LastName = "Doe",
            Username = "jane",
            Email = "jane@example.com",
            PasswordHash = "hash",
            SecurityStamp = "old-stamp",
            IsActive = true
        };

        _userRepository.GetByIdAsync("user-11", Arg.Any<CancellationToken>()).Returns(user);

        await _service.LogoutAllAsync("user-11");

        Assert.NotEqual("old-stamp", user.SecurityStamp);
        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _refreshSessionRepository.Received(1).RevokeActiveSessionsByUserIdAsync(
            "user-11",
            Arg.Any<DateTime>(),
            "logout_all",
            Arg.Any<CancellationToken>());
        await _authAuditRepository.Received(1).CreateAsync(
            Arg.Is<AuthAuditEvent>(x =>
                x.UserId == "user-11" &&
                x.EventType == "logout_all"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAuditTrailAsync_ReturnsProjectedAuditEntries()
    {
        var events = new List<AuthAuditEventDto>
        {
            new() { EventType = "login" },
            new() { EventType = "logout_all" }
        };

        _authAuditReader.GetRecentByUserIdAsync("user-12", 25, Arg.Any<CancellationToken>())
            .Returns(events);

        var result = await _service.GetAuditTrailAsync("user-12", 25);

        Assert.Equal(2, result.Count);
        await _authAuditReader.Received(1).GetRecentByUserIdAsync("user-12", 25, Arg.Any<CancellationToken>());
    }
}
