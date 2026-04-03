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
[Route("api/v1/[controller]")]
public class NotesController : ApiControllerBase
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
        var userId = RequireCurrentUserId();
        if (userId.Result is not null)
            return userId.Result;

        var created = await _noteService.CreateAsync(userId.Value!, request.ToDto(), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.ToResponse());
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        if (userId.Result is not null)
            return userId.Result;

        var notes = await _noteService.GetAllAsync(userId.Value!, cancellationToken);
        return Ok(notes.Select(x => x.ToResponse()));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        if (userId.Result is not null)
            return userId.Result;

        var note = await _noteService.GetByIdAsync(id, userId.Value!, cancellationToken);
        if (note is null)
            return this.NotFoundError("The requested note was not found.", ErrorCodes.NotesNotFound, "https://httpstatuses.com/404");

        return Ok(note.ToResponse());
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<NoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        if (userId.Result is not null)
            return userId.Result;

        var notes = await _noteService.SearchAsync(userId.Value!, q, cancellationToken);
        return Ok(notes.Select(x => x.ToResponse()));
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
        var userId = RequireCurrentUserId();
        if (userId.Result is not null)
            return userId.Result;

        var updated = await _noteService.UpdateAsync(id, userId.Value!, request.ToDto(), cancellationToken);

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
        var userId = RequireCurrentUserId();
        if (userId.Result is not null)
            return userId.Result;

        var updated = await _noteService.PatchAsync(id, userId.Value!, request.ToDto(), cancellationToken);

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
        var userId = RequireCurrentUserId();
        if (userId.Result is not null)
            return userId.Result;

        var deleted = await _noteService.DeleteAsync(id, userId.Value!, cancellationToken);
        return deleted
            ? NoContent()
            : this.NotFoundError("The requested note was not found.", ErrorCodes.NotesNotFound, "https://httpstatuses.com/404");
    }
}
