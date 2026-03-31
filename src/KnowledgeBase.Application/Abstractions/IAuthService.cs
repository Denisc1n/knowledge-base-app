using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.Abstractions;

public interface IAuthService
{
    Task<UserDto> SignupAsync(SignupUserDto dto, CancellationToken cancellationToken = default);
    Task<LoginResultDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<LoginResultDto> RefreshAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default);
    Task LogoutAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default);
}
