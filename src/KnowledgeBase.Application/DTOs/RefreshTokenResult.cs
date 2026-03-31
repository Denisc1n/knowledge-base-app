namespace KnowledgeBase.Application.DTOs;

public class RefreshTokenResult
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
}
