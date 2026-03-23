using KnowledgeBase.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeBase.Infrastructure.Persistence
{
    public class MongoContext
    {
        private readonly IMongoDatabase _database;
        private readonly MongoDbSettings _settings;

        public MongoContext(IOptions<MongoDbSettings> options)
        {
            _settings = options.Value;
            var client = new MongoClient(_settings.ConnectionString);
            _database = client.GetDatabase(_settings.DatabaseName);
        }

        public IMongoCollection<Note> Notes =>
            _database.GetCollection<Note>(_settings.NotesCollectionName);
    }
}
