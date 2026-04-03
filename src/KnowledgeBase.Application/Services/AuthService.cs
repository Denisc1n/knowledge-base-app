using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Exceptions;
using KnowledgeBase.Application.Security;
using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshSessionRepository _refreshSessionRepository;
    private readonly IRefreshSessionReader _refreshSessionReader;
    private readonly IAuthAuditRepository _authAuditRepository;
    private readonly IAuthAuditReader _authAuditReader;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenProvider _refreshTokenProvider;

    public AuthService(
        IUserRepository userRepository,
        IRefreshSessionReader refreshSessionReader,
        IAuthAuditRepository authAuditRepository,
        IAuthAuditReader authAuditReader,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshSessionRepository refreshSessionRepository,
        IRefreshTokenProvider refreshTokenProvider)
    {
        _userRepository = userRepository;
        _refreshSessionReader = refreshSessionReader;
        _authAuditRepository = authAuditRepository;
        _authAuditReader = authAuditReader;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshSessionRepository = refreshSessionRepository;
        _refreshTokenProvider = refreshTokenProvider;
    }

    public async Task<UserDto> SignupAsync(SignupUserDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = NormalizeUsername(dto.Username);
        var normalizedEmail = NormalizeEmail(dto.Email);

        if (await _userRepository.UsernameExistsAsync(normalizedUsername, cancellationToken))
            throw new DuplicateUserException("username", normalizedUsername);

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
            throw new DuplicateUserException("email", normalizedEmail);

        var user = new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Username = normalizedUsername,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            SecurityStamp = CreateSecurityStamp(),
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true,
            IsAdmin = false
        };

        var created = await _userRepository.CreateAsync(user, cancellationToken);
        return Map(created);
    }

    public async Task<LoginResultDto> LoginAsync(
        LoginDto dto,
        SessionContextDto context,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = NormalizeUsername(dto.Username);
        var user = await _userRepository.GetByUsernameAsync(normalizedUsername, cancellationToken);

        if (user is null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
            throw new AuthenticationException("Invalid username or password.");

        if (!user.IsActive)
            throw new AuthenticationException("This user is inactive.");

        return await IssueTokensAsync(user, context, AuthAuditEventTypes.Login, cancellationToken);
    }

    public async Task<LoginResultDto> RefreshAsync(
        RefreshTokenDto dto,
        SessionContextDto context,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenProvider.Hash(dto.RefreshToken);
        var session = await _refreshSessionRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (session is null || session.RevokedAtUtc.HasValue || session.ExpiresAtUtc <= DateTime.UtcNow)
            throw new AuthenticationException("Invalid or expired refresh token.");

        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new AuthenticationException("Invalid or expired refresh token.");

        var replacement = _refreshTokenProvider.Generate();
        var now = DateTime.UtcNow;
        session.RevokedAtUtc = now;
        session.RevokedReason = RefreshSessionRevocationReasons.Rotated;
        session.LastSeenAtUtc = now;
        session.LastSeenIp = context.IpAddress;
        session.ReplacedByTokenHash = _refreshTokenProvider.Hash(replacement.Token);

        await _refreshSessionRepository.UpdateAsync(session, cancellationToken);
        var replacementSession = new RefreshSession
        {
            UserId = user.Id,
            TokenHash = session.ReplacedByTokenHash,
            UserAgent = context.UserAgent,
            CreatedByIp = context.IpAddress,
            LastSeenIp = context.IpAddress,
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            ExpiresAtUtc = replacement.ExpiresAtUtc
        };

        await _refreshSessionRepository.CreateAsync(replacementSession, cancellationToken);

        await WriteAuditEventAsync(
            user.Id,
            replacementSession.Id,
            AuthAuditEventTypes.Refresh,
            "Refresh token rotated.",
            context,
            cancellationToken);

        var token = _jwtTokenGenerator.Generate(user);
        return new LoginResultDto
        {
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            RefreshToken = replacement.Token,
            RefreshTokenExpiresAtUtc = replacement.ExpiresAtUtc,
            User = Map(user)
        };
    }

    public async Task LogoutAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenProvider.Hash(dto.RefreshToken);
        var session = await _refreshSessionRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (session is null || session.RevokedAtUtc.HasValue)
            return;

        session.RevokedAtUtc = DateTime.UtcNow;
        session.RevokedReason = RefreshSessionRevocationReasons.Logout;
        await _refreshSessionRepository.UpdateAsync(session, cancellationToken);
        await WriteAuditEventAsync(
            session.UserId,
            session.Id,
            AuthAuditEventTypes.Logout,
            "Refresh session revoked by logout.",
            new SessionContextDto
            {
                IpAddress = session.LastSeenIp ?? session.CreatedByIp,
                UserAgent = session.UserAgent
            },
            cancellationToken);
    }

    public async Task ResetPasswordAsync(string userId, ResetPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new AuthenticationException("Invalid user.");

        if (!_passwordHasher.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new AuthenticationException("Current password is invalid.");

        user.PasswordHash = _passwordHasher.Hash(dto.NewPassword);
        user.SecurityStamp = CreateSecurityStamp();

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _refreshSessionRepository.RevokeActiveSessionsByUserIdAsync(
            user.Id,
            DateTime.UtcNow,
            RefreshSessionRevocationReasons.PasswordReset,
            cancellationToken);
        await WriteAuditEventAsync(
            user.Id,
            null,
            AuthAuditEventTypes.ResetPassword,
            "Password reset completed and active sessions revoked.",
            null,
            cancellationToken);
    }

    public async Task<IReadOnlyList<SessionDto>> GetSessionsAsync(
        string userId,
        string? currentRefreshToken,
        CancellationToken cancellationToken = default)
    {
        string? currentRefreshTokenHash = null;

        if (!string.IsNullOrWhiteSpace(currentRefreshToken))
            currentRefreshTokenHash = _refreshTokenProvider.Hash(currentRefreshToken);

        return await _refreshSessionReader.GetByUserIdAsync(userId, currentRefreshTokenHash, cancellationToken);
    }

    public async Task LogoutAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new AuthenticationException("Invalid user.");

        user.SecurityStamp = CreateSecurityStamp();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _refreshSessionRepository.RevokeActiveSessionsByUserIdAsync(
            user.Id,
            DateTime.UtcNow,
            RefreshSessionRevocationReasons.LogoutAll,
            cancellationToken);
        await WriteAuditEventAsync(
            user.Id,
            null,
            AuthAuditEventTypes.LogoutAll,
            "All active sessions were revoked.",
            null,
            cancellationToken);
    }

    public async Task<IReadOnlyList<AuthAuditEventDto>> GetAuditTrailAsync(
        string userId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await _authAuditReader.GetRecentByUserIdAsync(userId, limit, cancellationToken);
    }

    private static string NormalizeUsername(string username) =>
        username.Trim().ToLowerInvariant();

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static string CreateSecurityStamp() =>
        Guid.NewGuid().ToString("N");

    private static UserDto Map(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Username = user.Username,
        Email = user.Email,
        IsActive = user.IsActive,
        IsAdmin = user.IsAdmin
    };

    private async Task<LoginResultDto> IssueTokensAsync(
        User user,
        SessionContextDto context,
        string auditEventType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.SecurityStamp))
        {
            user.SecurityStamp = CreateSecurityStamp();
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        var accessToken = _jwtTokenGenerator.Generate(user);
        var refreshToken = _refreshTokenProvider.Generate();
        var now = DateTime.UtcNow;

        var session = new RefreshSession
        {
            UserId = user.Id,
            TokenHash = _refreshTokenProvider.Hash(refreshToken.Token),
            UserAgent = context.UserAgent,
            CreatedByIp = context.IpAddress,
            LastSeenIp = context.IpAddress,
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc
        };

        await _refreshSessionRepository.CreateAsync(session, cancellationToken);

        await WriteAuditEventAsync(
            user.Id,
            session.Id,
            auditEventType,
            "Session created.",
            context,
            cancellationToken);

        return new LoginResultDto
        {
            AccessToken = accessToken.AccessToken,
            ExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc,
            User = Map(user)
        };
    }

    private Task WriteAuditEventAsync(
        string userId,
        string? refreshSessionId,
        string eventType,
        string detail,
        SessionContextDto? context,
        CancellationToken cancellationToken)
    {
        return _authAuditRepository.CreateAsync(new AuthAuditEvent
        {
            UserId = userId,
            RefreshSessionId = refreshSessionId,
            EventType = eventType,
            Detail = detail,
            UserAgent = context?.UserAgent,
            IpAddress = context?.IpAddress,
            OccurredAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }
}
