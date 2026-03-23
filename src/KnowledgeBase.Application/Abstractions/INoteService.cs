using KnowledgeBase.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeBase.Application.Abstractions
{
    public interface INoteService
    {
        Task<NoteDto> CreateAsync(CreateNoteDto dto, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<NoteDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<NoteDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<NoteDto>> SearchAsync(string query, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(string id, UpdateNoteDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
        Task<bool> PatchAsync(string id, PatchNoteDto dto, CancellationToken cancellationToken = default);
    }
}
