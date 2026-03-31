namespace KnowledgeBase.Api.Contracts.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime RefreshAfterUtc { get; set; }
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public UserResponse User { get; set; } = default!;
}
