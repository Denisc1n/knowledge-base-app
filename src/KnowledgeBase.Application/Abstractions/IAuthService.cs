using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.Abstractions;

public interface IAuthService
{
    Task<UserDto> SignupAsync(SignupUserDto dto, CancellationToken cancellationToken = default);
    Task<LoginResultDto> LoginAsync(LoginDto dto, SessionContextDto context, CancellationToken cancellationToken = default);
    Task<LoginResultDto> RefreshAsync(RefreshTokenDto dto, SessionContextDto context, CancellationToken cancellationToken = default);
    Task LogoutAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(string userId, ResetPasswordDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionDto>> GetSessionsAsync(
        string userId,
        string? currentRefreshToken,
        CancellationToken cancellationToken = default);
    Task LogoutAllAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuthAuditEventDto>> GetAuditTrailAsync(
        string userId,
        int limit,
        CancellationToken cancellationToken = default);
}
