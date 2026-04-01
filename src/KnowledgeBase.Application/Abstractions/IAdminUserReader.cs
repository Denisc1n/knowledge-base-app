using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.Abstractions;

public interface IAdminUserReader
{
    Task<IReadOnlyList<UserListItemDto>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
