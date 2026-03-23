using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Api.Contracts.Notes
{
    public class PatchNoteRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public List<string>? Tags { get; set; }
        public string? Category { get; set; }
        public NoteStatus? Status { get; set; }
    }
}
