using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KnowledgeBase.Domain.Entities;

public class AuthAuditEvent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string? RefreshSessionId { get; set; }
    public string EventType { get; set; } = default!;
    public string? Detail { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
