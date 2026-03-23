using KnowledgeBase.Api.Contracts.Notes;
using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
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
    public async Task<IActionResult> Create(
        [FromBody] CreateNoteRequest request,
        CancellationToken cancellationToken)
    {
        var dto = new CreateNoteDto
        {
            Title = request.Title,
            Content = request.Content,
            Tags = request.Tags,
            Category = request.Category,
            Status = request.Status
        };

        var created = await _noteService.CreateAsync(dto, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, Map(created));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NoteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var notes = await _noteService.GetAllAsync(cancellationToken);
        return Ok(notes.Select(Map));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var note = await _noteService.GetByIdAsync(id, cancellationToken);
        if (note is null)
            return NotFound();

        return Ok(Map(note));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<NoteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken cancellationToken)
    {
        var notes = await _noteService.SearchAsync(q, cancellationToken);
        return Ok(notes.Select(Map));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateNoteRequest request,
        CancellationToken cancellationToken)
    {
        var dto = new UpdateNoteDto
        {
            Title = request.Title,
            Content = request.Content,
            Tags = request.Tags,
            Category = request.Category,
            Status = request.Status
        };

        var updated = await _noteService.UpdateAsync(id, dto, cancellationToken);

        return updated ? NoContent() : NotFound();
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(
    string id,
    [FromBody] PatchNoteRequest request,
    CancellationToken cancellationToken)
    {
        var dto = new PatchNoteDto
        {
            Title = request.Title,
            Content = request.Content,
            Tags = request.Tags,
            Category = request.Category,
            Status = request.Status
        };

        var updated = await _noteService.PatchAsync(id, dto, cancellationToken);

        return updated ? NoContent() : NotFound();
    }


    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var deleted = await _noteService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

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