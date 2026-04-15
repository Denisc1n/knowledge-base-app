using KnowledgeBase.Api.Contracts.Admin;
using KnowledgeBase.Api.Contracts.Auth;
using KnowledgeBase.Api.Extensions;
using KnowledgeBase.Api.Security;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Exceptions;
using KnowledgeBase.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/v1/admin/users")]
public class AdminUsersController : ApiControllerBase
{
    private readonly IAdminService _adminService;

    public AdminUsersController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [Authorize(Policy = AuthorizationPolicies.MasterAdminOnly)]
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.AuthSensitive)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAdmin(
        [FromBody] CreateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _adminService.CreateAdminAsync(request.ToDto(), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, created.ToResponse());
        }
        catch (DuplicateUserException ex)
        {
            return this.ConflictError(
                ex.Message,
                code: ErrorCodes.AuthDuplicateUser,
                type: "https://httpstatuses.com/409",
                field: ex.FieldName);
        }
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

    [Authorize(Policy = AuthorizationPolicies.MasterAdminOnly)]
    [HttpPost("{id}/promote-admin")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PromoteToAdmin(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _adminService.PromoteToAdminAsync(id, cancellationToken);
            if (updated is null)
                return this.NotFoundError("The requested user was not found.", ErrorCodes.UsersNotFound, "https://httpstatuses.com/404");

            return Ok(updated.ToResponse());
        }
        catch (InvalidAdminOperationException ex)
        {
            return this.ConflictError(
                ex.Message,
                code: ErrorCodes.AdminInvalidOperation,
                type: "https://httpstatuses.com/409");
        }
    }

    [Authorize(Policy = AuthorizationPolicies.MasterAdminOnly)]
    [HttpPost("{id}/demote-admin")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DemoteAdmin(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _adminService.DemoteAdminAsync(id, cancellationToken);
            if (updated is null)
                return this.NotFoundError("The requested user was not found.", ErrorCodes.UsersNotFound, "https://httpstatuses.com/404");

            return Ok(updated.ToResponse());
        }
        catch (InvalidAdminOperationException ex)
        {
            return this.ConflictError(
                ex.Message,
                code: ErrorCodes.AdminInvalidOperation,
                type: "https://httpstatuses.com/409");
        }
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
        try
        {
            var updated = await _adminService.SetUserActiveStatusAsync(id, request.IsActive, cancellationToken);
            if (updated is null)
                return this.NotFoundError("The requested user was not found.", ErrorCodes.UsersNotFound, "https://httpstatuses.com/404");

            return Ok(updated.ToResponse());
        }
        catch (InvalidAdminOperationException ex)
        {
            return this.ConflictError(
                ex.Message,
                code: ErrorCodes.AdminInvalidOperation,
                type: "https://httpstatuses.com/409");
        }
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
}
