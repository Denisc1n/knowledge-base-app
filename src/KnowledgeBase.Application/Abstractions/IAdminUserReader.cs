using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Queries;

namespace KnowledgeBase.Application.Abstractions;

public interface IAdminUserReader
{
    Task<IReadOnlyList<UserListItemDto>> GetAllAsync(GetUsersQuery query, CancellationToken cancellationToken = default);
}
