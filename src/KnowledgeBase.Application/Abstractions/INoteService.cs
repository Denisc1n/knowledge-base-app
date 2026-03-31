using KnowledgeBase.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeBase.Application.Abstractions
{
    public interface INoteService
    {
        Task<NoteDto> CreateAsync(string userId, CreateNoteDto dto, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<NoteDto>> GetAllAsync(string userId, CancellationToken cancellationToken = default);
        Task<NoteDto?> GetByIdAsync(string id, string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<NoteDto>> SearchAsync(string userId, string query, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(string id, string userId, UpdateNoteDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, string userId, CancellationToken cancellationToken = default);
        Task<bool> PatchAsync(string id, string userId, PatchNoteDto dto, CancellationToken cancellationToken = default);
    }
}
