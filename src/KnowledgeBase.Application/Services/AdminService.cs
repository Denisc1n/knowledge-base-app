using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Exceptions;
using KnowledgeBase.Application.Queries;
using KnowledgeBase.Application.Security;
using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserReader _userReader;
    private readonly INoteRepository _noteRepository;
    private readonly IRefreshSessionRepository _refreshSessionRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AdminService(
        IUserRepository userRepository,
        IUserReader userReader,
        INoteRepository noteRepository,
        IRefreshSessionRepository refreshSessionRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _userReader = userReader;
        _noteRepository = noteRepository;
        _refreshSessionRepository = refreshSessionRepository;
        _passwordHasher = passwordHasher;
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

        return await _userReader.GetAllAsync(normalizedQuery, cancellationToken);
    }

    public async Task<UserDto> CreateAdminAsync(
        CreateAdminUserDto dto,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = User.NormalizeUsername(dto.Username);
        var normalizedEmail = User.NormalizeEmail(dto.Email);

        if (await _userRepository.UsernameExistsAsync(normalizedUsername, cancellationToken))
            throw new DuplicateUserException("username", normalizedUsername);

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
            throw new DuplicateUserException("email", normalizedEmail);

        var user = User.Create(
            dto.FirstName,
            dto.LastName,
            dto.Username,
            dto.Email,
            _passwordHasher.Hash(dto.Password),
            DateTime.UtcNow,
            UserRole.Admin);

        var created = await _userRepository.CreateAsync(user, cancellationToken);
        return Map(created);
    }

    public async Task<UserDto?> PromoteToAdminAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return null;

        if (user.Role == UserRole.MasterAdmin)
            throw new InvalidAdminOperationException("The master admin role cannot be changed.");

        if (user.Role == UserRole.Admin)
            return Map(user);

        user.Role = UserRole.Admin;
        user.RotateSecurityStamp();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _refreshSessionRepository.RevokeActiveSessionsByUserIdAsync(
            user.Id,
            DateTime.UtcNow,
            RefreshSessionRevocationReasons.RoleChanged,
            cancellationToken);

        return Map(user);
    }

    public async Task<UserDto?> DemoteAdminAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return null;

        if (user.Role == UserRole.MasterAdmin)
            throw new InvalidAdminOperationException("The master admin cannot be demoted.");

        if (user.Role == UserRole.User)
            return Map(user);

        user.Role = UserRole.User;
        user.RotateSecurityStamp();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _refreshSessionRepository.RevokeActiveSessionsByUserIdAsync(
            user.Id,
            DateTime.UtcNow,
            RefreshSessionRevocationReasons.RoleChanged,
            cancellationToken);

        return Map(user);
    }

    public async Task<UserDto?> SetUserActiveStatusAsync(
        string userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return null;

        if (user.Role == UserRole.MasterAdmin && !isActive)
            throw new InvalidAdminOperationException("The master admin account cannot be deactivated.");

        if (user.IsActive == isActive)
            return Map(user);

        user.IsActive = isActive;
        await _userRepository.UpdateAsync(user, cancellationToken);

        if (!isActive)
        {
            await _refreshSessionRepository.RevokeActiveSessionsByUserIdAsync(
                user.Id,
                DateTime.UtcNow,
                RefreshSessionRevocationReasons.UserDeactivated,
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
        Role = user.Role
    };
}
