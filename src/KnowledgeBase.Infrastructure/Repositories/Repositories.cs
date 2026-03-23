using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;
using KnowledgeBase.Infrastructure.Persistence;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

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

        public async Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Notes
                .Find(_ => true)
                .SortByDescending(x => x.UpdatedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<Note?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _context.Notes
                .Find(x => x.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Note>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Array.Empty<Note>();

            var lowered = query.Trim().ToLowerInvariant();

            var filter = Builders<Note>.Filter.Or(
                Builders<Note>.Filter.Regex(x => x.Title, new MongoDB.Bson.BsonRegularExpression(lowered, "i")),
                Builders<Note>.Filter.Regex(x => x.Content, new MongoDB.Bson.BsonRegularExpression(lowered, "i")),
                Builders<Note>.Filter.AnyEq(x => x.Tags, lowered),
                Builders<Note>.Filter.Regex(x => x.Category, new MongoDB.Bson.BsonRegularExpression(lowered, "i"))
            );

            return await _context.Notes
                .Find(filter)
                .SortByDescending(x => x.UpdatedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> UpdateAsync(Note note, CancellationToken cancellationToken = default)
        {
            var result = await _context.Notes.ReplaceOneAsync(
                x => x.Id == note.Id,
                note,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var result = await _context.Notes.DeleteOneAsync(x => x.Id == id, cancellationToken);
            return result.DeletedCount > 0;
        }

        public async Task<bool> PatchAsync(
            string id,
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
                x => x.Id == id,
                combinedUpdate,
                cancellationToken: cancellationToken);

            return result.MatchedCount > 0;
        }
    }
}
