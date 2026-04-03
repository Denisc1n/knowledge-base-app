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
[Route("api/v1/[controller]")]
public class AuthController : ApiControllerBase
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
        try
        {
            var created = await _authService.SignupAsync(request.ToDto(), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, created.ToResponse());
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
        try
        {
            var result = await _authService.LoginAsync(request.ToDto(), CreateSessionContext(), cancellationToken);
            _logger.LogInformation(
                "User {Username} logged in successfully from {IpAddress}.",
                result.User.Username,
                HttpContext.Connection.RemoteIpAddress?.ToString());
            AppendRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
            return Ok(result.ToResponse());
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
            var result = await _authService.RefreshAsync(refreshToken.ToDto(), CreateSessionContext(), cancellationToken);

            _logger.LogInformation(
                "Refresh token rotation completed for user {UserId}.",
                result.User.Id);
            AppendRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
            return Ok(result.ToResponse());
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
            await _authService.LogoutAsync(refreshToken.ToDto(), cancellationToken);
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
        var userId = RequireCurrentUserId();
        if (userId.Result is not null)
            return userId.Result;

        var sessions = await _authService.GetSessionsAsync(
            userId.Value!,
            ResolveRefreshToken(null),
            cancellationToken);

        return Ok(sessions.Select(x => x.ToResponse()));
    }

    [Authorize(Policy = AuthorizationPolicies.AuthenticatedUser)]
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        if (userId.Result is not null)
            return userId.Result;

        try
        {
            await _authService.LogoutAllAsync(userId.Value!, cancellationToken);
            _logger.LogInformation("User {UserId} logged out all sessions.", userId.Value);
            DeleteRefreshTokenCookie();
            return NoContent();
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning(ex, "Logout-all failed for user {UserId}.", userId.Value);
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
        var userId = RequireCurrentUserId();
        if (userId.Result is not null)
            return userId.Result;

        try
        {
            await _authService.ResetPasswordAsync(userId.Value!, request.ToDto(), cancellationToken);

            _logger.LogInformation("Password reset completed for user {UserId}.", userId.Value);
            DeleteRefreshTokenCookie();
            return NoContent();
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning(ex, "Password reset failed for user {UserId}.", userId.Value);
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
        var userId = RequireCurrentUserId();
        if (userId.Result is not null)
            return userId.Result;

        var auditEvents = await _authService.GetAuditTrailAsync(userId.Value!, limit, cancellationToken);
        return Ok(auditEvents.Select(x => x.ToResponse()));
    }

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
}
