namespace KnowledgeBase.Application.DTOs;

public class LoginResultDto
{
    public string AccessToken { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = default!;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public UserDto User { get; set; } = default!;
}
