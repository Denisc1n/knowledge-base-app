using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Exceptions;
using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshSessionRepository _refreshSessionRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenProvider _refreshTokenProvider;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshSessionRepository refreshSessionRepository,
        IRefreshTokenProvider refreshTokenProvider)
    {
        _userRepository = userRepository;
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
            IsActive = true,
            IsAdmin = false
        };

        var created = await _userRepository.CreateAsync(user, cancellationToken);
        return Map(created);
    }

    public async Task<LoginResultDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = NormalizeUsername(dto.Username);
        var user = await _userRepository.GetByUsernameAsync(normalizedUsername, cancellationToken);

        if (user is null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
            throw new AuthenticationException("Invalid username or password.");

        if (!user.IsActive)
            throw new AuthenticationException("This user is inactive.");

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<LoginResultDto> RefreshAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenProvider.Hash(dto.RefreshToken);
        var session = await _refreshSessionRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (session is null || session.RevokedAtUtc.HasValue || session.ExpiresAtUtc <= DateTime.UtcNow)
            throw new AuthenticationException("Invalid or expired refresh token.");

        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new AuthenticationException("Invalid or expired refresh token.");

        var replacement = _refreshTokenProvider.Generate();
        session.RevokedAtUtc = DateTime.UtcNow;
        session.ReplacedByTokenHash = _refreshTokenProvider.Hash(replacement.Token);

        await _refreshSessionRepository.UpdateAsync(session, cancellationToken);
        await _refreshSessionRepository.CreateAsync(new RefreshSession
        {
            UserId = user.Id,
            TokenHash = session.ReplacedByTokenHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = replacement.ExpiresAtUtc
        }, cancellationToken);

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
        await _refreshSessionRepository.UpdateAsync(session, cancellationToken);
    }

    private static string NormalizeUsername(string username) =>
        username.Trim().ToLowerInvariant();

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

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

    private async Task<LoginResultDto> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenGenerator.Generate(user);
        var refreshToken = _refreshTokenProvider.Generate();

        await _refreshSessionRepository.CreateAsync(new RefreshSession
        {
            UserId = user.Id,
            TokenHash = _refreshTokenProvider.Hash(refreshToken.Token),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc
        }, cancellationToken);

        return new LoginResultDto
        {
            AccessToken = accessToken.AccessToken,
            ExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc,
            User = Map(user)
        };
    }
}
