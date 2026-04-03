using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Services
{
    public class NoteService : INoteService
    {
        private readonly INoteRepository _noteRepository;
        private readonly INoteReader _noteReader;

        public NoteService(INoteRepository noteRepository, INoteReader noteReader)
        {
            _noteRepository = noteRepository;
            _noteReader = noteReader;
        }

        public async Task<NoteDto> CreateAsync(string userId, CreateNoteDto dto, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var note = Note.Create(userId, dto.Title, dto.Content, dto.Tags, dto.Category, dto.Status, now);

            var created = await _noteRepository.CreateAsync(note, cancellationToken);

            return Map(created);
        }

        public async Task<IReadOnlyList<NoteDto>> GetAllAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _noteReader.GetAllAsync(userId, cancellationToken);
        }

        public async Task<NoteDto?> GetByIdAsync(string id, string userId, CancellationToken cancellationToken = default)
        {
            return await _noteReader.GetByIdAsync(id, userId, cancellationToken);
        }

        public async Task<IReadOnlyList<NoteDto>> SearchAsync(string userId, string query, CancellationToken cancellationToken = default)
        {
            return await _noteReader.SearchAsync(query, userId, cancellationToken);
        }

        public async Task<bool> UpdateAsync(string id, string userId, UpdateNoteDto dto, CancellationToken cancellationToken = default)
        {
            var existing = await _noteRepository.GetByIdAsync(id, userId, cancellationToken);
            if (existing is null)
                return false;

            existing.ApplyUpdate(dto.Title, dto.Content, dto.Tags, dto.Category, dto.Status, DateTime.UtcNow);

            return await _noteRepository.UpdateAsync(existing, cancellationToken);
        }

        public async Task<bool> PatchAsync(string id, string userId, PatchNoteDto dto, CancellationToken cancellationToken = default)
        {
            return await _noteRepository.PatchAsync(
                id,
                userId,
                Note.NormalizeOptional(dto.Title),
                Note.NormalizeOptional(dto.Content),
                Note.NormalizeOptionalTags(dto.Tags),
                Note.NormalizeOptional(dto.Category),
                dto.Status,
                DateTime.UtcNow,
                cancellationToken);
        }

        public Task<bool> DeleteAsync(string id, string userId, CancellationToken cancellationToken = default)
            => _noteRepository.DeleteAsync(id, userId, cancellationToken);

        private static NoteDto Map(Note note) => new()
        {
            Id = note.Id,
            Title = note.Title,
            Content = note.Content,
            Tags = note.Tags,
            Category = note.Category,
            Status = note.Status,
            CreatedAtUtc = note.CreatedAtUtc,
            UpdatedAtUtc = note.UpdatedAtUtc
        };
    }
}
