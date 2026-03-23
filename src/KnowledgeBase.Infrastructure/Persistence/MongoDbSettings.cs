using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeBase.Infrastructure.Persistence
{
    public class MongoDbSettings
    {
        public const string SectionName = "MongoDb";

        public string ConnectionString { get; set; } = default!;
        public string DatabaseName { get; set; } = default!;
        public string NotesCollectionName { get; set; } = "notes";
    }
}
