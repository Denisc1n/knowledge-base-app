using KnowledgeBase.Api.Contracts.Auth;
using KnowledgeBase.Api.Extensions;
using KnowledgeBase.Api.Observability;
using KnowledgeBase.Api.Security;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Exceptions;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly RefreshTokenSettings _refreshTokenSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger,
        IOptions<RefreshTokenSettings> refreshTokenOptions)
    {
        _authService = authService;
        _logger = logger;
        _refreshTokenSettings = refreshTokenOptions.Value;
    }

    [HttpPost("signup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Signup(
        [FromBody] SignupRequest request,
        CancellationToken cancellationToken)
    {
        var dto = new SignupUserDto
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Username = request.Username,
            Email = request.Email,
            Password = request.Password
        };

        try
        {
            var created = await _authService.SignupAsync(dto, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, Map(created));
        }
        catch (DuplicateUserException ex)
        {
            return this.ConflictError(
                ex.Message,
                code: ErrorCodes.AuthDuplicateUser,
                type: "https://httpstatuses.com/409",
                field: ex.FieldName);
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var dto = new LoginDto
        {
            Username = request.Username,
            Password = request.Password
        };

        try
        {
            var result = await _authService.LoginAsync(dto, CreateSessionContext(), cancellationToken);
            _logger.LogInformation(
                "User {Username} logged in successfully from {IpAddress}.",
                result.User.Username,
                HttpContext.Connection.RemoteIpAddress?.ToString());
            AppendRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
            return Ok(Map(result));
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning(
                ex,
                "Login failed for username {Username} from {IpAddress}.",
                request.Username,
                HttpContext.Connection.RemoteIpAddress?.ToString());
            return this.UnauthorizedError(
                ex.Message,
                code: ErrorCodes.AuthInvalidCredentials,
                type: "https://httpstatuses.com/401");
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest? request,
        CancellationToken cancellationToken)
    {
        var refreshToken = ResolveRefreshToken(request?.RefreshToken);
        if (string.IsNullOrWhiteSpace(refreshToken))
            return this.UnauthorizedError(
                "Refresh token is missing.",
                code: ErrorCodes.AuthRefreshTokenMissing,
                type: "https://httpstatuses.com/401");

        try
        {
            var result = await _authService.RefreshAsync(new RefreshTokenDto
            {
                RefreshToken = refreshToken
            }, CreateSessionContext(), cancellationToken);

            _logger.LogInformation(
                "Refresh token rotation completed for user {UserId}.",
                result.User.Id);
            AppendRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
            return Ok(Map(result));
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning(
                ex,
                "Refresh token request failed from {IpAddress}.",
                HttpContext.Connection.RemoteIpAddress?.ToString());
            DeleteRefreshTokenCookie();
            return this.UnauthorizedError(
                ex.Message,
                code: ErrorCodes.AuthInvalidCredentials,
                type: "https://httpstatuses.com/401");
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest? request,
        CancellationToken cancellationToken)
    {
        var refreshToken = ResolveRefreshToken(request?.RefreshToken);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await _authService.LogoutAsync(new RefreshTokenDto
            {
                RefreshToken = refreshToken
            }, cancellationToken);
            _logger.LogInformation(
                "Logout completed for refresh token request from {IpAddress}.",
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        DeleteRefreshTokenCookie();
        return NoContent();
    }

    [Authorize(Policy = AuthorizationPolicies.AuthenticatedUser)]
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IEnumerable<SessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var userId = User.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return this.UnauthorizedError(
                "User identifier is missing.",
                code: ErrorCodes.AuthMissingUserId,
                type: "https://httpstatuses.com/401");

        var sessions = await _authService.GetSessionsAsync(
            userId,
            ResolveRefreshToken(null),
            cancellationToken);

        return Ok(sessions.Select(Map));
    }

    [Authorize(Policy = AuthorizationPolicies.AuthenticatedUser)]
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userId = User.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return this.UnauthorizedError(
                "User identifier is missing.",
                code: ErrorCodes.AuthMissingUserId,
                type: "https://httpstatuses.com/401");

        try
        {
            await _authService.LogoutAllAsync(userId, cancellationToken);
            _logger.LogInformation("User {UserId} logged out all sessions.", userId);
            DeleteRefreshTokenCookie();
            return NoContent();
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning(ex, "Logout-all failed for user {UserId}.", userId);
            return this.UnauthorizedError(
                ex.Message,
                code: ErrorCodes.AuthInvalidCredentials,
                type: "https://httpstatuses.com/401");
        }
    }

    [Authorize(Policy = AuthorizationPolicies.AuthenticatedUser)]
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return this.UnauthorizedError(
                "User identifier is missing.",
                code: ErrorCodes.AuthMissingUserId,
                type: "https://httpstatuses.com/401");

        try
        {
            await _authService.ResetPasswordAsync(userId, new ResetPasswordDto
            {
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            }, cancellationToken);

            _logger.LogInformation("Password reset completed for user {UserId}.", userId);
            DeleteRefreshTokenCookie();
            return NoContent();
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning(ex, "Password reset failed for user {UserId}.", userId);
            return this.UnauthorizedError(
                ex.Message,
                code: ErrorCodes.AuthInvalidCredentials,
                type: "https://httpstatuses.com/401");
        }
    }

    [Authorize(Policy = AuthorizationPolicies.AuthenticatedUser)]
    [HttpGet("audit")]
    [ProducesResponseType(typeof(IEnumerable<AuthAuditEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditTrail(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return this.UnauthorizedError(
                "User identifier is missing.",
                code: ErrorCodes.AuthMissingUserId,
                type: "https://httpstatuses.com/401");

        var auditEvents = await _authService.GetAuditTrailAsync(userId, limit, cancellationToken);
        return Ok(auditEvents.Select(Map));
    }

    private LoginResponse Map(LoginResultDto dto) => new()
    {
        AccessToken = dto.AccessToken,
        ExpiresAtUtc = dto.ExpiresAtUtc,
        RefreshAfterUtc = dto.ExpiresAtUtc.AddMinutes(-1),
        RefreshTokenExpiresAtUtc = dto.RefreshTokenExpiresAtUtc,
        User = Map(dto.User)
    };

    private string? ResolveRefreshToken(string? requestRefreshToken)
    {
        if (!string.IsNullOrWhiteSpace(requestRefreshToken))
            return requestRefreshToken;

        return Request.Cookies.TryGetValue(_refreshTokenSettings.CookieName, out var cookieRefreshToken)
            ? cookieRefreshToken
            : null;
    }

    private SessionContextDto CreateSessionContext() => new()
    {
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        UserAgent = Request.Headers.UserAgent.ToString()
    };

    private void AppendRefreshTokenCookie(string refreshToken, DateTime expiresAtUtc)
    {
        var sameSite = ParseSameSiteMode(_refreshTokenSettings.CookieSameSite);
        var secure = _refreshTokenSettings.CookieSecure || Request.IsHttps || sameSite == SameSiteMode.None;

        Response.Cookies.Append(_refreshTokenSettings.CookieName, refreshToken, new CookieOptions
        {
            HttpOnly = _refreshTokenSettings.CookieHttpOnly,
            Secure = secure,
            SameSite = sameSite,
            Expires = expiresAtUtc,
            IsEssential = true,
            Path = "/"
        });
    }

    private void DeleteRefreshTokenCookie()
    {
        var sameSite = ParseSameSiteMode(_refreshTokenSettings.CookieSameSite);
        var secure = _refreshTokenSettings.CookieSecure || Request.IsHttps || sameSite == SameSiteMode.None;

        Response.Cookies.Delete(_refreshTokenSettings.CookieName, new CookieOptions
        {
            HttpOnly = _refreshTokenSettings.CookieHttpOnly,
            Secure = secure,
            SameSite = sameSite,
            IsEssential = true,
            Path = "/"
        });
    }

    private static SameSiteMode ParseSameSiteMode(string sameSite)
    {
        return Enum.TryParse<SameSiteMode>(sameSite, true, out var parsed)
            ? parsed
            : SameSiteMode.Lax;
    }

    private static UserResponse Map(UserDto dto) => new()
    {
        Id = dto.Id,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Username = dto.Username,
        Email = dto.Email,
        IsActive = dto.IsActive,
        IsAdmin = dto.IsAdmin
    };

    private static SessionResponse Map(SessionDto dto) => new()
    {
        Id = dto.Id,
        UserAgent = dto.UserAgent,
        IpAddress = dto.IpAddress,
        CreatedAtUtc = dto.CreatedAtUtc,
        LastSeenAtUtc = dto.LastSeenAtUtc,
        ExpiresAtUtc = dto.ExpiresAtUtc,
        RevokedAtUtc = dto.RevokedAtUtc,
        RevokedReason = dto.RevokedReason,
        IsCurrent = dto.IsCurrent,
        IsActive = dto.IsActive
    };

    private static AuthAuditEventResponse Map(AuthAuditEventDto dto) => new()
    {
        EventType = dto.EventType,
        Detail = dto.Detail,
        UserAgent = dto.UserAgent,
        IpAddress = dto.IpAddress,
        OccurredAtUtc = dto.OccurredAtUtc
    };
}
