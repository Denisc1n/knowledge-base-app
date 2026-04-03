using KnowledgeBase.Api.Contracts.Notes;
using KnowledgeBase.Api.Extensions;
using KnowledgeBase.Api.Security;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AuthenticatedUser)]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;

    public NotesController(INoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateNoteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return this.UnauthorizedError("User identifier is missing.", ErrorCodes.AuthMissingUserId, "https://httpstatuses.com/401");

        var dto = new CreateNoteDto
        {
            Title = request.Title,
            Content = request.Content,
            Tags = request.Tags,
            Category = request.Category,
            Status = request.Status
        };

        var created = await _noteService.CreateAsync(userId, dto, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, Map(created));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return this.UnauthorizedError("User identifier is missing.", ErrorCodes.AuthMissingUserId, "https://httpstatuses.com/401");

        var notes = await _noteService.GetAllAsync(userId, cancellationToken);
        return Ok(notes.Select(Map));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return this.UnauthorizedError("User identifier is missing.", ErrorCodes.AuthMissingUserId, "https://httpstatuses.com/401");

        var note = await _noteService.GetByIdAsync(id, userId, cancellationToken);
        if (note is null)
            return this.NotFoundError("The requested note was not found.", ErrorCodes.NotesNotFound, "https://httpstatuses.com/404");

        return Ok(Map(note));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<NoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return this.UnauthorizedError("User identifier is missing.", ErrorCodes.AuthMissingUserId, "https://httpstatuses.com/401");

        var notes = await _noteService.SearchAsync(userId, q, cancellationToken);
        return Ok(notes.Select(Map));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateNoteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return this.UnauthorizedError("User identifier is missing.", ErrorCodes.AuthMissingUserId, "https://httpstatuses.com/401");

        var dto = new UpdateNoteDto
        {
            Title = request.Title,
            Content = request.Content,
            Tags = request.Tags,
            Category = request.Category,
            Status = request.Status
        };

        var updated = await _noteService.UpdateAsync(id, userId, dto, cancellationToken);

        return updated
            ? NoContent()
            : this.NotFoundError("The requested note was not found.", ErrorCodes.NotesNotFound, "https://httpstatuses.com/404");
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Patch(
        string id,
        [FromBody] PatchNoteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return this.UnauthorizedError("User identifier is missing.", ErrorCodes.AuthMissingUserId, "https://httpstatuses.com/401");

        var dto = new PatchNoteDto
        {
            Title = request.Title,
            Content = request.Content,
            Tags = request.Tags,
            Category = request.Category,
            Status = request.Status
        };

        var updated = await _noteService.PatchAsync(id, userId, dto, cancellationToken);

        return updated
            ? NoContent()
            : this.NotFoundError("The requested note was not found.", ErrorCodes.NotesNotFound, "https://httpstatuses.com/404");
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return this.UnauthorizedError("User identifier is missing.", ErrorCodes.AuthMissingUserId, "https://httpstatuses.com/401");

        var deleted = await _noteService.DeleteAsync(id, userId, cancellationToken);
        return deleted
            ? NoContent()
            : this.NotFoundError("The requested note was not found.", ErrorCodes.NotesNotFound, "https://httpstatuses.com/404");
    }

    private string? GetCurrentUserId() => User.GetCurrentUserId();

    private static NoteResponse Map(NoteDto dto) => new()
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
