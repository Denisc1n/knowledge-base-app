namespace KnowledgeBase.Application.DTOs;

public class TokenResult
{
    public string AccessToken { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
}
