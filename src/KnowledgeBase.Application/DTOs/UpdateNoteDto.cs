using KnowledgeBase.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeBase.Application.DTOs
{
    public class UpdateNoteDto
    {
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public List<string> Tags { get; set; } = new();
        public string Category { get; set; } = default!;
        public NoteStatus Status { get; set; }
    }
}
