using KnowledgeBase.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeBase.Domain.Abstractions
{
    public interface INoteRepository
    {
        Task<Note> CreateAsync(Note note, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Note?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Note>> SearchAsync(string query, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Note note, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
        Task<bool> PatchAsync(
       string id,
       string? title,
       string? content,
       List<string>? tags,
       string? category,
       KnowledgeBase.Domain.Enums.NoteStatus? status,
       DateTime updatedAtUtc,
       CancellationToken cancellationToken = default);
    }
}
