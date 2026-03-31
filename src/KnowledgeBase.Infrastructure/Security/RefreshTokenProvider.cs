using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace KnowledgeBase.Infrastructure.Security;

public class RefreshTokenProvider : IRefreshTokenProvider
{
    private readonly RefreshTokenSettings _settings;

    public RefreshTokenProvider(IOptions<RefreshTokenSettings> options)
    {
        _settings = options.Value;
    }

    public RefreshTokenResult Generate()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(_settings.TokenByteLength);

        return new RefreshTokenResult
        {
            Token = Convert.ToBase64String(tokenBytes),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_settings.ExpirationDays)
        };
    }

    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
