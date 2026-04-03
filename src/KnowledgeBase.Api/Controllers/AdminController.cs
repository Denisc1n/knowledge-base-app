using KnowledgeBase.Api.Contracts.Admin;
using KnowledgeBase.Api.Contracts.Auth;
using KnowledgeBase.Api.Extensions;
using KnowledgeBase.Api.Security;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("admin/users")]
[Route("api/v1/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminUsersController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetUsersQuery query,
        CancellationToken cancellationToken)
    {
        var users = await _adminService.GetAllUsersAsync(query, cancellationToken);
        return Ok(users);
    }

    [HttpPatch("{id}/active")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetUserActiveStatus(
        string id,
        [FromBody] SetUserActiveStatusRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _adminService.SetUserActiveStatusAsync(id, request.IsActive, cancellationToken);
        if (updated is null)
            return this.NotFoundError("The requested user was not found.", ErrorCodes.UsersNotFound, "https://httpstatuses.com/404");

        return Ok(Map(updated));
    }

    [HttpDelete("{userId}/notes/{noteId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUserNote(
        string userId,
        string noteId,
        CancellationToken cancellationToken)
    {
        var deleted = await _adminService.DeleteUserNoteAsync(userId, noteId, cancellationToken);
        return deleted
            ? NoContent()
            : this.NotFoundError("The requested note was not found.", ErrorCodes.NotesNotFound, "https://httpstatuses.com/404");
    }

    private static UserResponse Map(UserDto dto) => new()
    {
        Id = dto.Id,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Username = dto.Username,
        Email = dto.Email,
        IsActive = dto.IsActive,
        IsAdmin = dto.IsAdmin
    };
}
