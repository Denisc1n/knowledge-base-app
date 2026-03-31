namespace KnowledgeBase.Infrastructure.Persistence;

public class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = default!;
    public string DatabaseName { get; set; } = default!;
    public string NotesCollectionName { get; set; } = "notes";
    public string UsersCollectionName { get; set; } = "users";
    public string RefreshSessionsCollectionName { get; set; } = "refreshSessions";
}
