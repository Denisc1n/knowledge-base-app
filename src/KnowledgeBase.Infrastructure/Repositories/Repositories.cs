using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;
using KnowledgeBase.Infrastructure.Persistence;
using MongoDB.Driver;

namespace KnowledgeBase.Infrastructure.Repositories
{
    public class NoteRepository : INoteRepository
    {
        private readonly MongoContext _context;

        public NoteRepository(MongoContext context)
        {
            _context = context;
        }

        public async Task<Note> CreateAsync(Note note, CancellationToken cancellationToken = default)
        {
            await _context.Notes.InsertOneAsync(note, cancellationToken: cancellationToken);
            return note;
        }

        public async Task<Note?> GetByIdAsync(string id, string userId, CancellationToken cancellationToken = default)
        {
            return await _context.Notes
                .Find(x => x.Id == id && x.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> UpdateAsync(Note note, CancellationToken cancellationToken = default)
        {
            var result = await _context.Notes.ReplaceOneAsync(
                x => x.Id == note.Id && x.UserId == note.UserId,
                note,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id, string userId, CancellationToken cancellationToken = default)
        {
            var result = await _context.Notes.DeleteOneAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
            return result.DeletedCount > 0;
        }

        public async Task<bool> PatchAsync(
            string id,
            string userId,
            string? title,
            string? content,
            List<string>? tags,
            string? category,
            NoteStatus? status,
            DateTime updatedAtUtc,
            CancellationToken cancellationToken = default)
        {
            var updates = new List<UpdateDefinition<Note>>();

            if (title is not null)
                updates.Add(Builders<Note>.Update.Set(x => x.Title, title));

            if (content is not null)
                updates.Add(Builders<Note>.Update.Set(x => x.Content, content));

            if (tags is not null)
                updates.Add(Builders<Note>.Update.Set(x => x.Tags, tags));

            if (category is not null)
                updates.Add(Builders<Note>.Update.Set(x => x.Category, category));

            if (status.HasValue)
                updates.Add(Builders<Note>.Update.Set(x => x.Status, status.Value));

            updates.Add(Builders<Note>.Update.Set(x => x.UpdatedAtUtc, updatedAtUtc));

            var combinedUpdate = Builders<Note>.Update.Combine(updates);

            var result = await _context.Notes.UpdateOneAsync(
                x => x.Id == id && x.UserId == userId,
                combinedUpdate,
                cancellationToken: cancellationToken);

            return result.MatchedCount > 0;
        }
    }
}
