using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KnowledgeBase.Domain.Entities;

public class RefreshSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string TokenHash { get; set; } = default!;
    public string? UserAgent { get; set; }
    public string? CreatedByIp { get; set; }
    public string? LastSeenIp { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedReason { get; set; }
    public string? ReplacedByTokenHash { get; set; }
}
