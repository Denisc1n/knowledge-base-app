using KnowledgeBase.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KnowledgeBase.Infrastructure.Persistence;

public class MongoContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbSettings _settings;

    public MongoContext(IOptions<MongoDbSettings> options, IMongoClient client)
    {
        _settings = options.Value;
        _database = client.GetDatabase(_settings.DatabaseName);
        EnsureIndexes();
    }

    public IMongoCollection<Note> Notes =>
        _database.GetCollection<Note>(_settings.NotesCollectionName);

    public IMongoCollection<User> Users =>
        _database.GetCollection<User>(_settings.UsersCollectionName);

    public IMongoCollection<RefreshSession> RefreshSessions =>
        _database.GetCollection<RefreshSession>(_settings.RefreshSessionsCollectionName);

    public IMongoCollection<AuthAuditEvent> AuthAuditEvents =>
        _database.GetCollection<AuthAuditEvent>("authAuditEvents");

    private void EnsureIndexes()
    {
        var usernameIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(x => x.Username),
            new CreateIndexOptions { Unique = true });

        var emailIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(x => x.Email),
            new CreateIndexOptions { Unique = true });

        Users.Indexes.CreateMany([usernameIndex, emailIndex]);

        var noteUserIdIndex = new CreateIndexModel<Note>(
            Builders<Note>.IndexKeys.Ascending(x => x.UserId));

        Notes.Indexes.CreateOne(noteUserIdIndex);

        var tokenHashIndex = new CreateIndexModel<RefreshSession>(
            Builders<RefreshSession>.IndexKeys.Ascending(x => x.TokenHash),
            new CreateIndexOptions { Unique = true });

        var userIdIndex = new CreateIndexModel<RefreshSession>(
            Builders<RefreshSession>.IndexKeys.Ascending(x => x.UserId));

        RefreshSessions.Indexes.CreateMany([tokenHashIndex, userIdIndex]);

        var authAuditUserIdIndex = new CreateIndexModel<AuthAuditEvent>(
            Builders<AuthAuditEvent>.IndexKeys.Ascending(x => x.UserId));

        var authAuditOccurredAtIndex = new CreateIndexModel<AuthAuditEvent>(
            Builders<AuthAuditEvent>.IndexKeys.Descending(x => x.OccurredAtUtc));

        AuthAuditEvents.Indexes.CreateMany([authAuditUserIdIndex, authAuditOccurredAtIndex]);
    }
}
