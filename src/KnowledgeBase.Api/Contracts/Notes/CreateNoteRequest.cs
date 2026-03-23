using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Api.Contracts.Notes
{
    public class CreateNoteRequest
    {
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public List<string> Tags { get; set; } = new();
        public string Category { get; set; } = default!;
        public NoteStatus Status { get; set; } = NoteStatus.Draft;
    }
}
