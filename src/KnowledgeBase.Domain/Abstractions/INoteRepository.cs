using KnowledgeBase.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeBase.Domain.Abstractions
{
    public interface INoteRepository
    {
        Task<Note> CreateAsync(Note note, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Note>> GetAllAsync(string userId, CancellationToken cancellationToken = default);
        Task<Note?> GetByIdAsync(string id, string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Note>> SearchAsync(string query, string userId, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Note note, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, string userId, CancellationToken cancellationToken = default);
        Task<bool> PatchAsync(
            string id,
            string userId,
            string? title,
            string? content,
            List<string>? tags,
            string? category,
            KnowledgeBase.Domain.Enums.NoteStatus? status,
            DateTime updatedAtUtc,
            CancellationToken cancellationToken = default);
    }
}
