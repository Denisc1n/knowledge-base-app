using KnowledgeBase.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KnowledgeBase.Domain.Entities;

public class Note
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public string UserId { get; set; } = default!;

    public string Title { get; set; } = default!;

    public string Content { get; set; } = default!;

    public List<string> Tags { get; set; } = new();

    public string Category { get; set; } = default!;

    public NoteStatus Status { get; set; } = NoteStatus.Draft;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public static Note Create(
        string userId,
        string title,
        string content,
        IEnumerable<string> tags,
        string category,
        NoteStatus status,
        DateTime nowUtc)
    {
        return new Note
        {
            UserId = userId,
            Title = NormalizeRequired(title),
            Content = NormalizeRequired(content),
            Tags = NormalizeTags(tags),
            Category = NormalizeRequired(category),
            Status = status,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }

    public void ApplyUpdate(
        string title,
        string content,
        IEnumerable<string> tags,
        string category,
        NoteStatus status,
        DateTime updatedAtUtc)
    {
        Title = NormalizeRequired(title);
        Content = NormalizeRequired(content);
        Tags = NormalizeTags(tags);
        Category = NormalizeRequired(category);
        Status = status;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static string? NormalizeOptional(string? value) => value?.Trim();

    public static List<string>? NormalizeOptionalTags(IEnumerable<string>? tags) =>
        tags is null ? null : NormalizeTags(tags);

    private static string NormalizeRequired(string value) => value.Trim();

    private static List<string> NormalizeTags(IEnumerable<string> tags) =>
        [.. tags
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()];
}
