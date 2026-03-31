namespace KnowledgeBase.Infrastructure.Persistence;

public class RefreshTokenSettings
{
    public const string SectionName = "RefreshToken";

    public int ExpirationDays { get; set; } = 7;
    public int TokenByteLength { get; set; } = 32;
    public string CookieName { get; set; } = "kb.refreshToken";
    public string CookieSameSite { get; set; } = "Lax";
    public bool CookieHttpOnly { get; set; } = true;
    public bool CookieSecure { get; set; }
}
