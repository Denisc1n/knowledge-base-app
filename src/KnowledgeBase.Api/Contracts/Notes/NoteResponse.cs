using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Api.Contracts.Notes
{
    public class NoteResponse
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public List<string> Tags { get; set; } = new();
        public string Category { get; set; } = default!;
        public NoteStatus Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
