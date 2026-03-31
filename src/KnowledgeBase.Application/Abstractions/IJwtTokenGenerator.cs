using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Abstractions;

public interface IJwtTokenGenerator
{
    TokenResult Generate(User user);
}
