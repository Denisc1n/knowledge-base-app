using KnowledgeBase.Api.Contracts.Auth;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Exceptions;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly RefreshTokenSettings _refreshTokenSettings;

    public AuthController(
        IAuthService authService,
        IOptions<RefreshTokenSettings> refreshTokenOptions)
    {
        _authService = authService;
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
            return Conflict(new { message = ex.Message, field = ex.FieldName });
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
            var result = await _authService.LoginAsync(dto, cancellationToken);
            AppendRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
            return Ok(Map(result));
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(new { message = ex.Message });
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
            return Unauthorized(new { message = "Refresh token is missing." });

        try
        {
            var result = await _authService.RefreshAsync(new RefreshTokenDto
            {
                RefreshToken = refreshToken
            }, cancellationToken);

            AppendRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
            return Ok(Map(result));
        }
        catch (AuthenticationException ex)
        {
            DeleteRefreshTokenCookie();
            return Unauthorized(new { message = ex.Message });
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
        }

        DeleteRefreshTokenCookie();
        return NoContent();
    }

    [Authorize]
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "User identifier is missing." });

        try
        {
            await _authService.ResetPasswordAsync(userId, new ResetPasswordDto
            {
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            }, cancellationToken);

            DeleteRefreshTokenCookie();
            return NoContent();
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
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
}
