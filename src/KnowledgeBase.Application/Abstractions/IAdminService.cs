using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Queries;

namespace KnowledgeBase.Application.Abstractions;

public interface IAdminService
{
    Task<IReadOnlyList<UserListItemDto>> GetAllUsersAsync(GetUsersQuery query, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAdminAsync(CreateAdminUserDto dto, CancellationToken cancellationToken = default);
    Task<UserDto?> PromoteToAdminAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserDto?> DemoteAdminAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserDto?> SetUserActiveStatusAsync(string userId, bool isActive, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserNoteAsync(string userId, string noteId, CancellationToken cancellationToken = default);
}
