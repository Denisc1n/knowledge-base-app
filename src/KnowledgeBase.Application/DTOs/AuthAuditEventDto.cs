namespace KnowledgeBase.Application.DTOs;

public class AuthAuditEventDto
{
    public string EventType { get; set; } = default!;
    public string? Detail { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
