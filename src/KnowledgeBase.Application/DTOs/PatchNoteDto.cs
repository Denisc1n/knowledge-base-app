using KnowledgeBase.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeBase.Application.DTOs
{
    public class PatchNoteDto
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public List<string>? Tags { get; set; }
        public string? Category { get; set; }
        public NoteStatus? Status { get; set; }
    }
}
