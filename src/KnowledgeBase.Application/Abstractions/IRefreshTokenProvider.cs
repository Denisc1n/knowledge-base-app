using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.Abstractions;

public interface IRefreshTokenProvider
{
    RefreshTokenResult Generate();
    string Hash(string token);
}
