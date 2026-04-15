using KnowledgeBase.Api.Contracts.Admin;
using KnowledgeBase.Api.Contracts.Auth;
using KnowledgeBase.Api.Contracts.Notes;
using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Api.Extensions;

public static class ApiContractMappingExtensions
{
    public static SignupUserDto ToDto(this SignupRequest request) => new()
    {
        FirstName = request.FirstName,
        LastName = request.LastName,
        Username = request.Username,
        Email = request.Email,
        Password = request.Password
    };

    public static LoginDto ToDto(this LoginRequest request) => new()
    {
        Username = request.Username,
        Password = request.Password
    };

    public static CreateAdminUserDto ToDto(this CreateAdminUserRequest request) => new()
    {
        FirstName = request.FirstName,
        LastName = request.LastName,
        Username = request.Username,
        Email = request.Email,
        Password = request.Password
    };

    public static RefreshTokenDto ToDto(this string refreshToken) => new()
    {
        RefreshToken = refreshToken
    };

    public static ResetPasswordDto ToDto(this ResetPasswordRequest request) => new()
    {
        CurrentPassword = request.CurrentPassword,
        NewPassword = request.NewPassword
    };

    public static CreateNoteDto ToDto(this CreateNoteRequest request) => new()
    {
        Title = request.Title,
        Content = request.Content,
        Tags = request.Tags,
        Category = request.Category,
        Status = request.Status
    };

    public static UpdateNoteDto ToDto(this UpdateNoteRequest request) => new()
    {
        Title = request.Title,
        Content = request.Content,
        Tags = request.Tags,
        Category = request.Category,
        Status = request.Status
    };

    public static PatchNoteDto ToDto(this PatchNoteRequest request) => new()
    {
        Title = request.Title,
        Content = request.Content,
        Tags = request.Tags,
        Category = request.Category,
        Status = request.Status
    };

    public static LoginResponse ToResponse(this LoginResultDto dto) => new()
    {
        AccessToken = dto.AccessToken,
        ExpiresAtUtc = dto.ExpiresAtUtc,
        RefreshAfterUtc = dto.ExpiresAtUtc.AddMinutes(-1),
        RefreshTokenExpiresAtUtc = dto.RefreshTokenExpiresAtUtc,
        User = dto.User.ToResponse()
    };

    public static UserResponse ToResponse(this UserDto dto) => new()
    {
        Id = dto.Id,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Username = dto.Username,
        Email = dto.Email,
        IsActive = dto.IsActive,
        Role = dto.Role.ToString()
    };

    public static SessionResponse ToResponse(this SessionDto dto) => new()
    {
        Id = dto.Id,
        UserAgent = dto.UserAgent,
        IpAddress = dto.IpAddress,
        CreatedAtUtc = dto.CreatedAtUtc,
        LastSeenAtUtc = dto.LastSeenAtUtc,
        ExpiresAtUtc = dto.ExpiresAtUtc,
        RevokedAtUtc = dto.RevokedAtUtc,
        RevokedReason = dto.RevokedReason,
        IsCurrent = dto.IsCurrent,
        IsActive = dto.IsActive
    };

    public static AuthAuditEventResponse ToResponse(this AuthAuditEventDto dto) => new()
    {
        EventType = dto.EventType,
        Detail = dto.Detail,
        UserAgent = dto.UserAgent,
        IpAddress = dto.IpAddress,
        OccurredAtUtc = dto.OccurredAtUtc
    };

    public static NoteResponse ToResponse(this NoteDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Content = dto.Content,
        Tags = dto.Tags,
        Category = dto.Category,
        Status = dto.Status,
        CreatedAtUtc = dto.CreatedAtUtc,
        UpdatedAtUtc = dto.UpdatedAtUtc
    };
}
