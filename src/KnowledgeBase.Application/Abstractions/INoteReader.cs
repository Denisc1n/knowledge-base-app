using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.Abstractions;

public interface INoteReader
{
    Task<IReadOnlyList<NoteDto>> GetAllAsync(string userId, CancellationToken cancellationToken = default);
    Task<NoteDto?> GetByIdAsync(string id, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NoteDto>> SearchAsync(string query, string userId, CancellationToken cancellationToken = default);
}
