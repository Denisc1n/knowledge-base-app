using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KnowledgeBase.Infrastructure.Queries;

public class NoteReader : INoteReader
{
    private readonly MongoContext _context;

    public NoteReader(MongoContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<NoteDto>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notes
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.UpdatedAtUtc)
            .Project(ProjectToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<NoteDto?> GetByIdAsync(
        string id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notes
            .Find(x => x.Id == id && x.UserId == userId)
            .Project(ProjectToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NoteDto>> SearchAsync(
        string query,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<NoteDto>();

        var lowered = query.Trim().ToLowerInvariant();

        var ownershipFilter = Builders<Note>.Filter.Eq(x => x.UserId, userId);
        var contentFilter = Builders<Note>.Filter.Or(
            Builders<Note>.Filter.Regex(x => x.Title, new BsonRegularExpression(lowered, "i")),
            Builders<Note>.Filter.Regex(x => x.Content, new BsonRegularExpression(lowered, "i")),
            Builders<Note>.Filter.AnyEq(x => x.Tags, lowered),
            Builders<Note>.Filter.Regex(x => x.Category, new BsonRegularExpression(lowered, "i"))
        );

        return await _context.Notes
            .Find(Builders<Note>.Filter.And(ownershipFilter, contentFilter))
            .SortByDescending(x => x.UpdatedAtUtc)
            .Project(ProjectToDto())
            .ToListAsync(cancellationToken);
    }

    private static ProjectionDefinition<Note, NoteDto> ProjectToDto()
    {
        return Builders<Note>.Projection.Expression(x => new NoteDto
        {
            Id = x.Id,
            Title = x.Title,
            Content = x.Content,
            Tags = x.Tags,
            Category = x.Category,
            Status = x.Status,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc
        });
    }
}
