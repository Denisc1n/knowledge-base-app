namespace KnowledgeBase.Api.Contracts.Auth;

public class SessionResponse
{
    public string Id { get; set; } = default!;
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedReason { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsActive { get; set; }
}
