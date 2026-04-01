using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Queries;
using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminUserReader _adminUserReader;
    private readonly INoteRepository _noteRepository;
    private readonly IRefreshSessionRepository _refreshSessionRepository;

    public AdminService(
        IUserRepository userRepository,
        IAdminUserReader adminUserReader,
        INoteRepository noteRepository,
        IRefreshSessionRepository refreshSessionRepository)
    {
        _userRepository = userRepository;
        _adminUserReader = adminUserReader;
        _noteRepository = noteRepository;
        _refreshSessionRepository = refreshSessionRepository;
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetAllUsersAsync(
        GetUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = new GetUsersQuery
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Max(1, query.PageSize),
            IsActive = query.IsActive,
            IsAdmin = query.IsAdmin,
            CreatedDate = query.CreatedDate,
            SortBy = Enum.IsDefined(query.SortBy)
                ? query.SortBy
                : UserSortBy.CreatedDate,
            SortDirection = Enum.IsDefined(query.SortDirection)
                ? query.SortDirection
                : SortDirection.Desc
        };

        return await _adminUserReader.GetAllAsync(normalizedQuery, cancellationToken);
    }

    public async Task<UserDto?> SetUserActiveStatusAsync(
        string userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return null;

        if (user.IsActive == isActive)
            return Map(user);

        user.IsActive = isActive;
        await _userRepository.UpdateAsync(user, cancellationToken);

        if (!isActive)
        {
            await _refreshSessionRepository.RevokeActiveSessionsByUserIdAsync(
                user.Id,
                DateTime.UtcNow,
                cancellationToken);
        }

        return Map(user);
    }

    public Task<bool> DeleteUserNoteAsync(
        string userId,
        string noteId,
        CancellationToken cancellationToken = default)
        => _noteRepository.DeleteAsync(noteId, userId, cancellationToken);

    private static UserDto Map(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Username = user.Username,
        Email = user.Email,
        IsActive = user.IsActive,
        IsAdmin = user.IsAdmin
    };
}
