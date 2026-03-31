using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Services
{
    public class NoteService : INoteService
    {
        private readonly INoteRepository _noteRepository;

        public NoteService(INoteRepository noteRepository)
        {
            _noteRepository = noteRepository;
        }

        public async Task<NoteDto> CreateAsync(string userId, CreateNoteDto dto, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var note = new Note
            {
                UserId = userId,
                Title = dto.Title.Trim(),
                Content = dto.Content.Trim(),
                Tags = [.. dto.Tags
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim().ToLowerInvariant())
                    .Distinct()],
                Category = dto.Category.Trim(),
                Status = dto.Status,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            var created = await _noteRepository.CreateAsync(note, cancellationToken);

            return Map(created);
        }

        public async Task<IReadOnlyList<NoteDto>> GetAllAsync(string userId, CancellationToken cancellationToken = default)
        {
            var notes = await _noteRepository.GetAllAsync(userId, cancellationToken);
            return notes.Select(Map).ToList();
        }

        public async Task<NoteDto?> GetByIdAsync(string id, string userId, CancellationToken cancellationToken = default)
        {
            var note = await _noteRepository.GetByIdAsync(id, userId, cancellationToken);
            return note is null ? null : Map(note);
        }

        public async Task<IReadOnlyList<NoteDto>> SearchAsync(string userId, string query, CancellationToken cancellationToken = default)
        {
            var notes = await _noteRepository.SearchAsync(query, userId, cancellationToken);
            return notes.Select(Map).ToList();
        }

        public async Task<bool> UpdateAsync(string id, string userId, UpdateNoteDto dto, CancellationToken cancellationToken = default)
        {
            var existing = await _noteRepository.GetByIdAsync(id, userId, cancellationToken);
            if (existing is null)
                return false;

            existing.Title = dto.Title.Trim();
            existing.Content = dto.Content.Trim();
            existing.Tags = dto.Tags
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();
            existing.Category = dto.Category.Trim();
            existing.Status = dto.Status;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            return await _noteRepository.UpdateAsync(existing, cancellationToken);
        }

        public async Task<bool> PatchAsync(string id, string userId, PatchNoteDto dto, CancellationToken cancellationToken = default)
        {
            var normalizedTitle = dto.Title?.Trim();
            var normalizedContent = dto.Content?.Trim();
            var normalizedCategory = dto.Category?.Trim();

            List<string>? normalizedTags = null;

            if (dto.Tags is not null)
            {
                normalizedTags = dto.Tags
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToList();
            }

            return await _noteRepository.PatchAsync(
                id,
                userId,
                normalizedTitle,
                normalizedContent,
                normalizedTags,
                normalizedCategory,
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
